# Deploy no Azure (Leva 8)

Guia para publicar a biblioteca no Azure com **sem senha nas App Settings**: os segredos ficam no **Key Vault** e o **App Service** os lê por **Managed Identity**. As migrations são aplicadas por um **script idempotente** no deploy.

> Estes passos dependem da sua conta Azure e precisam da CLI `az` autenticada (`az login`). O código e o pipeline já estão prontos no repositório; falta provisionar os recursos e preencher os nomes/segredos.

## Visão geral

- **App Service (Linux, .NET 10)** roda o app, com uma **identidade gerenciada** atribuída pelo sistema.
- **PostgreSQL Flexible Server** guarda os dados.
- **Key Vault** guarda os segredos (connection string, dados do primeiro bibliotecário, `EmailApiKey`).
- O App Service recebe só uma App Setting: `KeyVault__Uri`. O resto vem do cofre.

## 1. Variáveis de apoio

```bash
RG=library-rg
LOC=brazilsouth
APP=library-app-$RANDOM        # precisa ser único no Azure
PLAN=library-plan
PG=library-pg-$RANDOM          # precisa ser único
KV=library-kv-$RANDOM          # precisa ser único
PG_ADMIN=libadmin
PG_PASSWORD='UMA_SENHA_FORTE'  # usada só para criar o servidor e aplicar migrations
```

## 2. Grupo de recursos

```bash
az group create -n $RG -l $LOC
```

## 3. PostgreSQL Flexible Server

```bash
az postgres flexible-server create \
  --resource-group $RG --name $PG --location $LOC \
  --admin-user $PG_ADMIN --admin-password "$PG_PASSWORD" \
  --tier Burstable --sku-name Standard_B1ms \
  --version 16 --storage-size 32 \
  --public-access 0.0.0.0   # libera "Allow Azure services"; ajuste conforme sua política

az postgres flexible-server db create \
  --resource-group $RG --server-name $PG --database-name library
```

A connection string de produção fica assim (note o `Ssl Mode=Require`):

```text
Host=$PG.postgres.database.azure.com;Port=5432;Database=library;Username=$PG_ADMIN;Password=$PG_PASSWORD;Ssl Mode=Require
```

## 4. Key Vault e os segredos

```bash
az keyvault create -n $KV -g $RG -l $LOC

CONN="Host=$PG.postgres.database.azure.com;Port=5432;Database=library;Username=$PG_ADMIN;Password=$PG_PASSWORD;Ssl Mode=Require"

az keyvault secret set --vault-name $KV --name "ConnectionStrings--DefaultConnection" --value "$CONN"
az keyvault secret set --vault-name $KV --name "SeedLibrarian--Email"    --value "voce@exemplo.com"
az keyvault secret set --vault-name $KV --name "SeedLibrarian--Password" --value "UMA_SENHA_FORTE_DO_BIBLIOTECARIO"
az keyvault secret set --vault-name $KV --name "SeedLibrarian--Name"     --value "Seu Nome"
# Quando houver provedor de e-mail:
# az keyvault secret set --vault-name $KV --name "EmailApiKey" --value "..."
```

Nos nomes dos segredos, `--` vira `:` na configuração (`ConnectionStrings--DefaultConnection` → `ConnectionStrings:DefaultConnection`).

## 5. App Service com Managed Identity

```bash
az appservice plan create -g $RG -n $PLAN --is-linux --sku B1

az webapp create -g $RG -p $PLAN -n $APP --runtime "DOTNETCORE:10.0"

# Identidade gerenciada atribuída pelo sistema
az webapp identity assign -g $RG -n $APP
PRINCIPAL_ID=$(az webapp identity show -g $RG -n $APP --query principalId -o tsv)

# Dá à identidade permissão de leitura dos segredos do cofre (RBAC)
KV_ID=$(az keyvault show -n $KV --query id -o tsv)
az role assignment create \
  --assignee-object-id $PRINCIPAL_ID --assignee-principal-type ServicePrincipal \
  --role "Key Vault Secrets User" --scope $KV_ID

# Única App Setting: o endereço do cofre (sem nenhuma senha aqui)
az webapp config appsettings set -g $RG -n $APP \
  --settings KeyVault__Uri="https://$KV.vault.azure.net/"
```

> Se o cofre estiver no modo de políticas de acesso (não RBAC), troque o `az role assignment create` por
> `az keyvault set-policy -n $KV --object-id $PRINCIPAL_ID --secret-permissions get list`.

## 6. Migrations (script idempotente)

As migrations **não** são aplicadas pelo app na subida; rode o script idempotente apontando para o banco de produção:

```bash
dotnet ef migrations script --idempotent -o migrate.sql
psql "$CONN" -f migrate.sql
```

O script cria a tabela `__EFMigrationsHistory` se faltar e aplica só as migrations ainda não registradas, então é seguro rodar de novo.

## 7. Deploy

Manual:

```bash
dotnet publish Library.csproj -c Release -o publish
cd publish && zip -r ../app.zip . && cd ..
az webapp deploy -g $RG -n $APP --src-path app.zip --type zip
```

Automático: o workflow [.github/workflows/deploy.yml](../.github/workflows/deploy.yml) faz publish → gera e aplica o script de migrations → deploy. Para ele funcionar:

1. Preencha `AZURE_WEBAPP_NAME` no workflow com o nome do App Service.
2. Configure a federação OIDC (um app registration com credencial federada para este repositório) e cadastre os secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
3. Cadastre o secret `MIGRATIONS_CONNECTION` com a connection string usada só para aplicar migrations.

## O que depende da sua conta Azure

Tudo neste guia (passos 1–7) roda na sua assinatura. O repositório já traz: a carga do Key Vault no `Program.cs`, o workflow de deploy e este guia. Falta você: criar os recursos, cadastrar os segredos, preencher o nome do App Service no workflow e configurar a federação OIDC.
