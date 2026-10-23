# Leva 6 — Testes ✅

**Concluída em:** 2026-10-23 · **Branch:** `leva-6` (PR para `main`)

## O que foi feito
- Novo projeto de testes `tests/Library.Tests` (xUnit), referenciando o projeto `Library`.
- Nova solução `Library.slnx` reunindo o app (`Library`) e os testes (`Library.Tests`).
- `DatabaseFixture`: sobe o **esquema real** (todas as migrations) em um banco PostgreSQL **dedicado de teste**, uma vez por execução. Cada teste roda em esquema limpo (`TRUNCATE` das tabelas antes de cada caso) e usa seu próprio `DbContext`, como uma requisição.
- `LoanServiceTests`: 9 testes do `LoanService` cobrindo as regras de empréstimo e devolução.

## Por que PostgreSQL real (e não banco em memória)
As regras do `LoanService` dependem de recursos que um provedor em memória (InMemory/SQLite) não reproduz fielmente:
- o **índice único parcial** `IX_Loans_BookCopyId_Open` (um único empréstimo aberto por exemplar);
- os **CHECK constraints** (`CK_Loans_DueDate`, `CK_Loans_ReturnedAt`);
- o **enum gravado como string** e a captura do erro `23505` do Npgsql.

Testar contra o Postgres de verdade garante que a regra e o banco concordam.

## Casos cobertos (9)
- **Prazo de 14 dias:** emprestar define `DueDate = data do empréstimo + 14` e marca o exemplar como `Loaned`.
- **Prazo configurável:** com `LoanRules.LoanPeriodDays = 7`, o prazo passa a ser 7 dias.
- **Limite de 3:** o 4º empréstimo aberto do mesmo leitor é barrado; o exemplar recusado continua `Available`.
- **Bloqueio por atraso:** leitor com empréstimo aberto e vencido não pega outro.
- **Exemplar já emprestado (status):** segundo pedido para um exemplar `Loaned` é barrado pela checagem de status, antes do banco.
- **Corrida no mesmo exemplar (índice único):** com um empréstimo aberto já no banco e o exemplar ainda `Available`, o segundo pedido passa pela checagem de status e é barrado pelo índice único parcial, virando a mensagem "Este exemplar já está emprestado." (nunca um 500).
- **Devolução:** preenche `ReturnedAt` e volta o exemplar para `Available`.
- **Devolução dupla:** devolver o mesmo empréstimo de novo retorna "já foi devolvido".
- **Exemplar perdido na devolução:** exemplar marcado como `Lost` enquanto emprestado continua `Lost` após a devolução (não é forçado para `Available`).

## Como rodar os testes
Os testes precisam de um PostgreSQL acessível e usam um banco **separado** (`library_test`), então nunca tocam no banco `library` do dia a dia.

Com o container `library-db` ligado, defina a conexão e rode:

```bash
# opção 1: string de conexão completa
export LIBRARY_TEST_DB="Host=localhost;Port=5433;Database=library_test;Username=admin;Password=SUA_SENHA"
dotnet test

# opção 2: reaproveitar a senha do .env (mesma do POSTGRES_PASSWORD)
export POSTGRES_PASSWORD=SUA_SENHA   # monta Host=localhost;Port=5433;Database=library_test;Username=admin
dotnet test
```

O `DatabaseFixture` cria o banco `library_test` do zero (`EnsureDeleted` + `Migrate`) no início da execução, com índices e CHECKs iguais aos de produção.

## Decisões
- **Banco de teste dedicado, não Testcontainers:** o `PLANO.md` dava as duas opções. Optou-se por um banco de teste (string de conexão por variável de ambiente) por rodar em qualquer ambiente com Postgres acessível, sem exigir o daemon do Docker disponível para a suíte (aqui o Docker exige `sudo`). Testcontainers fica como evolução futura para CI isolado.
- **Isolamento por `TRUNCATE ... RESTART IDENTITY`** antes de cada teste, com `DbContext` novo por operação, reproduzindo o ciclo de vida de uma requisição e evitando estado compartilhado entre casos.
- **Simulação da corrida:** para exercitar o `catch` do `23505` de forma determinística, um empréstimo aberto é inserido direto no banco com o exemplar ainda `Available`; assim o serviço passa pela checagem de status e é o índice único que barra o segundo.

## Validação
`dotnet build` da solução limpo (0 avisos, 0 erros). `dotnet test` → **9/9 aprovados** contra um PostgreSQL 16 real, pelos dois caminhos de configuração (`LIBRARY_TEST_DB` e `POSTGRES_PASSWORD`).

## Ponto de partida da Leva 7 (Reset de senha)
- `ResetPasswordDto` + token com expiração (tabela `PasswordResetTokens` ou Data Protection).
- Envio de e-mail via serviço externo (`EmailApiKey` nos User Secrets / Key Vault).
