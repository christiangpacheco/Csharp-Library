# Plano de desenvolvimento em levas

Cada leva é um pedaço pequeno que compila, roda e termina com um commit. Só começamos a próxima quando a anterior estiver funcionando no navegador.

Estado de partida (commit da Leva 0): models `User`, `Book`, `BookCopy` e `Loan`, configurations, `LibraryDbContext` e migration `InitialCreate` aplicada no PostgreSQL local (Docker, porta 5433). O resto da estrutura existe, mas está vazio.

---

## Leva 0 — Base de dados ✅
- [x] Models, enums e configurations
- [x] Migration `InitialCreate` (Users, Books, BookCopies, Loans)
- [x] PostgreSQL em Docker + User Secrets
- [x] Commit

## Leva 1 — Esqueleto do site ✅
Objetivo: abrir `/` sem erro e ter um layout comum.
- [x] `Views/_ViewStart.cshtml`, `Views/_ViewImports.cshtml`, `Views/Shared/_Layout.cshtml` (Bootstrap via CDN, menu Livros / Empréstimos / Entrar)
- [x] `HomeController` com a action `Error` e a view `Views/Shared/Error.cshtml`, que o `Program.cs` já referencia
- [x] Corrigir `BooksController`: `return View(books)` em vez de `/Views/Account/Books/...`
- [x] `docker-compose.yml` com o PostgreSQL (porta 5433, volume `library-data`)
- [x] Atualizar o README (4 tabelas, porta 5433, Docker)

**Pronto quando:** `dotnet run` abre a lista de livros com o layout.

## Leva 2 — Catálogo de livros (CRUD) ✅
Objetivo: cadastrar e editar livros de verdade no banco.
- [x] DTOs `CreateBookDto` e `UpdateBookDto` (renomear `UpdateBook.cs`), com `[Required]`, `[StringLength]` e ISBN de 13 dígitos
- [x] `IBookService` / `BookService` usando `LibraryDbContext` (listar, detalhar, criar, editar, desativar/reativar, excluir)
- [x] Registrar no `Program.cs` com `AddScoped<IBookService, BookService>()`
- [x] `BooksController`: Index, Details, Create, Edit (GET/POST) com views
- [x] ISBN duplicado vira mensagem de validação, não erro 500
- [x] Busca por título/autor/ISBN e paginação na Index

- [x] Ativo/inativo (`IsActive`) e soft delete (`DeletedAt`) em todas as tabelas, migration `AddActiveAndSoftDelete`

**Pronto quando:** dá para cadastrar, editar e ver livros, e eles continuam lá depois de reiniciar a aplicação.

## Leva 3 — Exemplares (BookCopies)
Objetivo: cada livro mostra seus exemplares e quantos estão disponíveis.
- [ ] Na página Details do livro: lista de exemplares (código e status) e formulário "Adicionar exemplar"
- [ ] Marcar exemplar como `Lost`; Desativar/Reativar (`IsActive`); excluir só exemplar inativo (soft delete)
- [ ] Na Index: coluna "Disponíveis X de Y"

**Pronto quando:** um livro com 3 exemplares mostra "3 de 3 disponíveis".

## Leva 4 — Contas e login ✅
Objetivo: usuário se cadastra, entra e sai; só o bibliotecário altera o acervo.
- [x] DTOs `RegisterUserDto` e `LoginDto`
- [x] `IAuthService` / `AuthService` com `PasswordHasher<User>` e e-mail salvo em minúsculas
- [x] Cookie auth no `Program.cs` (`AddAuthentication().AddCookie()` + `UseAuthentication()` antes de `UseAuthorization()`)
- [x] `AccountController`: Register, Login, Logout e AccessDenied
- [x] `[Authorize(Roles = "Librarian")]` em Create e Edit de livros e exemplares
- [x] Seed do primeiro bibliotecário, com a senha vinda dos User Secrets (nunca no código)

**Pronto quando:** um usuário comum vê o catálogo mas recebe "acesso negado" ao tentar cadastrar um livro.

## Leva 5 — Empréstimos e devoluções ✅
Objetivo: o fluxo principal da biblioteca, com as regras de negócio.
- [x] DTOs `CreateLoanDto` e `LoanResponseDto`
- [x] `ILoanService` / `LoanService`, com as regras abaixo:
  - só empresta exemplar `Available`
  - máximo de 3 empréstimos abertos por usuário
  - bloqueia quem tem empréstimo atrasado
  - prazo de 14 dias (`DueDate = hoje + 14`)
  - empréstimo e mudança de status do exemplar na mesma transação
  - devolução preenche `ReturnedAt` e volta o exemplar para `Available`, e devolver duas vezes é erro
- [x] Limites (3 e 14) no `appsettings.json`, seção `LoanRules`
- [x] `LoansController`: "Meus empréstimos" (User) e Emprestar, Devolver e Atrasados (Librarian)
- [x] Tratar a violação do índice `IX_Loans_BookCopyId_Open` como "exemplar já emprestado"
- [x] Ajuste do "X de Y" para não contar exemplar inativo (pendência da Leva 4)

**Pronto quando:** dá para emprestar, ver o atraso e devolver, e as regras barram os casos inválidos.

## Leva 6 — Testes ✅
- [x] Projeto `Library.Tests` (xUnit) + solução `Library.slnx`
- [x] Testes do `LoanService` com PostgreSQL real (banco de teste dedicado `library_test` via `LIBRARY_TEST_DB`)
- [x] Casos: limite de 3, atraso bloqueia, exemplar já emprestado (índice único), devolução dupla, prazo de 14 dias

**Pronto quando:** `dotnet test` roda verde contra um PostgreSQL real. Relatório em `docs/levas/leva-6.md`.

## Leva 7 — Reset de senha ✅
- [x] `ResetPasswordDto` + token com expiração (tabela `PasswordResetTokens`, token guardado como hash SHA-256, uso único)
- [x] `IEmailSender` com o link de redefinição (no dev, o link vai para o log; remetente de produção com `EmailApiKey` fica para a Leva 8, junto do Key Vault)

**Pronto quando:** dá para pedir "Esqueci minha senha", receber o link e redefinir; token inválido, expirado ou já usado é recusado. Relatório em `docs/levas/leva-7.md`.

## Leva 8 — Azure ✅
- [x] Key Vault no `Program.cs` fora de Development (`Azure.Extensions.AspNetCore.Configuration.Secrets` + `Azure.Identity`)
- [x] App Service com Managed Identity + PostgreSQL Flexible Server (provisionamento scriptado em `docs/azure-deploy.md`; execução depende da conta Azure)
- [x] Migrations via `dotnet ef migrations script --idempotent` no pipeline (`.github/workflows/deploy.yml`)
- [x] README de produção atualizado (sem senha nas App Settings — só `KeyVault__Uri`)

**Pronto quando:** o código lê os segredos do Key Vault por Managed Identity e há guia + workflow para provisionar e publicar.

> **Decisão (2026-10-23):** o projeto não será publicado no Azure. O código do Key Vault, o guia `docs/azure-deploy.md` e o workflow ficam como referência; o workflow roda só manualmente e o app é testado localmente (PostgreSQL e Mailpit em Docker). Relatório em `docs/levas/leva-8.md`.

---

## Regras para todas as levas
- Nada é apagado do banco: toda tabela tem `DeletedAt` (soft delete, filtro global no `LibraryDbContext`). Users, Books e BookCopies também têm `IsActive`: desativar é uma ação própria, e só registro inativo pode ser excluído. Empréstimo tem só `DeletedAt` (o estado dele é aberto/devolvido). Índices únicos valem só para registros não excluídos. Toda entidade nova segue o mesmo padrão.
- Nunca editar `Migrations/` à mão: mudar model/configuration e rodar `dotnet ef migrations add NomeDaMudanca`.
- Controllers finos: regra de negócio fica nos services.
- Cada leva termina com `dotnet build` limpo e um commit `leva N: descrição`.
- Ao concluir uma leva, criar o relatório `docs/levas/leva-N.md` (o que foi feito, decisões, validação, pendências e ponto de partida da próxima) e atualizar `docs/levas/README.md`. Para retomar o trabalho, leia o `PLANO.md` e o relatório da última leva.
