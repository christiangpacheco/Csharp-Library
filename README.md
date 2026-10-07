# Library

Aplicacao web para gerenciamento de livros, desenvolvida com ASP.NET Core MVC, .NET 10, Entity Framework Core e PostgreSQL.

## Requisitos

- SDK do .NET 10
- Docker (para o PostgreSQL 17) ou um PostgreSQL instalado localmente
- Banco PostgreSQL chamado `library` na porta `5433`
- Usuario PostgreSQL `admin` com permissao no banco

Verifique o SDK instalado:

```bash
dotnet --version
```

## Configuracao do banco local

### Subir o PostgreSQL com Docker

O `docker-compose.yml` na raiz sobe o PostgreSQL 17 no container `library-db`, na porta `5433`, com os dados no volume `library-data`. A senha nao fica no arquivo: ela vem da variavel `POSTGRES_PASSWORD`.

```bash
cp .env.example .env   # edite o .env e defina POSTGRES_PASSWORD
docker compose up -d
```

O arquivo `.env` esta no `.gitignore` e nao deve ser commitado. Use a mesma senha nos User Secrets (abaixo).

Se preferir sem compose, o equivalente com `docker run` e:

```bash
docker run -d --name library-db -e POSTGRES_DB=library -e POSTGRES_USER=admin \
  -e POSTGRES_PASSWORD=SUA_SENHA -p 5433:5432 -v library-data:/var/lib/postgresql/data postgres:17
```

Se ja existe um container `library-db` criado com `docker run`, nao rode o `docker compose up` ao mesmo tempo: os dois usam o mesmo nome e a mesma porta.

### Sem Docker

Crie o banco `library` no PostgreSQL. Neste projeto, a conexao local usada pelos User Secrets e:

```text
Host=localhost;Port=5433;Database=library;Username=admin;Password=SUA_SENHA
```

A porta, o usuario e a senha precisam corresponder a instalacao local do PostgreSQL. Se o seu PostgreSQL usa a porta `5432`, altere `5433` no comando abaixo.

## User Secrets

Os User Secrets armazenam a senha fora do repositorio. Execute os comandos na raiz do projeto, onde esta o arquivo `Library.csproj`.

Inicialize os User Secrets uma vez:

```bash
dotnet user-secrets init
```

Cadastre a connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=library;Username=admin;Password=SUA_SENHA"
```

Para conferir se a chave foi cadastrada:

```bash
dotnet user-secrets list
```

Nao coloque a senha no `appsettings.json` ou no `appsettings.Development.json`.

## Restaurar dependencias

```bash
dotnet restore
```

O projeto ja possui os pacotes principais:

- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.Design`

## Entity Framework Core

Instale a ferramenta `dotnet-ef` compativel com o EF Core 10:

```bash
dotnet tool install --global dotnet-ef --version 10.0.11
```

Se a ferramenta ja estiver instalada, atualize-a quando necessario:

```bash
dotnet tool update --global dotnet-ef --version 10.0.11
```

### Ordem correta para a primeira configuracao

1. Configure o PostgreSQL local.
2. Configure os User Secrets.
3. Aplique as migrations no banco:

```bash
dotnet ef database update
```

A migration `InitialCreate` ja existe neste projeto e cria as tabelas `Users`, `Books`, `BookCopies` e `Loans`, alem da tabela interna `__EFMigrationsHistory`. Nao execute `dotnet ef migrations add InitialCreate` novamente.

Nunca edite os arquivos da pasta `Migrations/` a mao. Para mudar o banco, altere o model ou a configuration e gere uma migration nova.

Quando o model mudar, crie uma nova migration com outro nome:

```bash
dotnet ef migrations add NomeDaAlteracao
dotnet ef database update
```

## Executar a aplicacao

Para executar em modo de desenvolvimento:

```bash
dotnet run
```

A aplicacao iniciara em uma das URLs exibidas no terminal, normalmente:

```text
https://localhost:7xxx
http://localhost:5xxx
```

A rota inicial esta configurada para `Books/Index`, entao a pagina de livros pode ser acessada pela raiz `/` ou por:

```text
/Books/Index
```

Para compilar sem executar:

```bash
dotnet build
```

## DBeaver

Para visualizar as tabelas, conecte o DBeaver usando os mesmos dados dos User Secrets:

```text
Host: localhost
Port: 5433
Database: library
Username: admin
```

No navegador do DBeaver, atualize:

```text
Schemas > public > Tables
```

As tabelas criadas pela migration sao `Users`, `Books`, `BookCopies` e `Loans`. Para consulta SQL, use aspas por causa das letras maiusculas:

```sql
SELECT * FROM public."Books";
```

Para verificar as migrations aplicadas:

```sql
SELECT * FROM public."__EFMigrationsHistory";
```

## Configuracao de producao no Azure

Em producao, nao use User Secrets. Configure a connection string no Azure App Service em:

```text
Configuration > Application settings
```

Crie a variavel:

```text
ConnectionStrings__DefaultConnection
```

Com o valor do PostgreSQL do Azure, por exemplo:

```text
Host=servidor.postgres.database.azure.com;Port=5432;Database=library;Username=USUARIO;Password=SENHA;Ssl Mode=Require
```

O Azure usa dois sublinhados para representar os dois-pontos da configuracao:

```text
ConnectionStrings__DefaultConnection
```

equivale a:

```text
ConnectionStrings:DefaultConnection
```

Antes de executar a aplicacao em producao, aplique as migrations no banco do Azure usando uma connection string de producao configurada com seguranca:

```bash
dotnet ef database update
```

## Estado atual

Neste momento, o projeto possui:

- ASP.NET Core MVC configurado;
- .NET 10;
- PostgreSQL com Entity Framework Core;
- `LibraryDbContext` com a entidade `Book`;
- migration inicial `InitialCreate`;
- tabela `Books` criada pela migration;
- User Secrets configurados para o banco local.
