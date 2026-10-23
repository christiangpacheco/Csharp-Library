# Leva 8 — Azure ✅

**Concluída em:** 2026-10-23 · **Branch:** `leva-8` (PR para `main`)

Leva de infraestrutura e deploy. O que é código e documentação foi feito e validado na nuvem; o provisionamento em si depende da conta Azure do Christian (ver o fim).

## O que foi feito
- **Key Vault no `Program.cs`:** fora de Development, a aplicação carrega os segredos do Azure Key Vault com `DefaultAzureCredential` (Managed Identity). O endereço do cofre vem da App Setting `KeyVault:Uri`/`KeyVault__Uri`; sem ela, nada é carregado (dev segue nos User Secrets). Pacotes `Azure.Identity` (1.21.0) e `Azure.Extensions.AspNetCore.Configuration.Secrets` (1.5.2).
- **Pipeline de deploy:** `.github/workflows/deploy.yml` — publish, geração e aplicação do **script idempotente** de migrations e deploy para o App Service (login OIDC, sem senha).
- **Guia de provisionamento:** `docs/azure-deploy.md` — passo a passo com `az` para Key Vault, PostgreSQL Flexible Server, App Service com Managed Identity, atribuição de acesso ao cofre (RBAC `Key Vault Secrets User`) e os segredos.
- **README de produção reescrito:** a seção antiga punha a senha direto nas App Settings; agora a única App Setting é `KeyVault__Uri` e os segredos ficam no cofre (tabela com os nomes `ConnectionStrings--DefaultConnection`, `SeedLibrarian--*`).

## Decisões
- **Segredos no Key Vault, não nas App Settings:** nenhuma senha fica em configuração visível; o App Service lê o cofre pela identidade gerenciada. Nos nomes dos segredos, `--` vale por `:`.
- **Migrations fora do app:** a aplicação não aplica migrations na subida; o pipeline gera `migrate.sql` com `dotnet ef migrations script --idempotent` e aplica com `psql`. O script é seguro de rodar de novo (só aplica o que falta) e usa uma conexão dedicada (secret `MIGRATIONS_CONNECTION`), separada do acesso do app.
- **OIDC no deploy:** o workflow usa federação (`azure/login` com `id-token`), sem publish profile nem senha no GitHub.
- **E-mail de produção:** o `IEmailSender` de produção (provedor externo lendo `EmailApiKey`, que também iria no cofre) continua pendente — falta escolher o provedor. Em dev, o link de redefinição vai para o log (Leva 7).

## Validação
- `dotnet build` limpo (0 avisos, 0 erros) com a carga do Key Vault e os pacotes Azure.
- `dotnet ef migrations script --idempotent` gera o script com as três migrations (`InitialCreate`, `AddActiveAndSoftDelete`, `AddPasswordResetTokens`) e a tabela `__EFMigrationsHistory`.
- O provisionamento (`az ...`) e o deploy não foram executados aqui: dependem da conta Azure. O guia e o workflow estão prontos para rodar.

## O que depende da conta Azure do Christian
1. Criar os recursos seguindo `docs/azure-deploy.md` (grupo, PostgreSQL Flexible, Key Vault, App Service).
2. Cadastrar os segredos no Key Vault (connection string, `SeedLibrarian--*`, no futuro `EmailApiKey`).
3. Atribuir à identidade gerenciada do App Service o papel `Key Vault Secrets User`.
4. Definir a App Setting `KeyVault__Uri`.
5. Preencher `AZURE_WEBAPP_NAME` no workflow e configurar a federação OIDC + os secrets do repositório (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `MIGRATIONS_CONNECTION`).

## Pendências / observações
- `IEmailSender` de produção (provedor + `EmailApiKey`) ainda por implementar.
- O guia libera "Allow Azure services" no PostgreSQL por simplicidade; endurecer a rede (VNet/Private Endpoint) fica como melhoria.

## Fim do plano de levas
Com a Leva 8, o `PLANO.md` está completo (Levas 0–8). Próximos passos ficam a critério do Christian (ex.: provedor de e-mail, endurecimento de rede, testes de integração adicionais).
