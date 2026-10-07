# Plano de desenvolvimento em levas

Cada leva é um pedaço pequeno que compila, roda e termina com um commit. Só começamos a próxima quando a anterior estiver funcionando no navegador.

Estado de partida (commit da Leva 0): models `User`, `Book`, `BookCopy` e `Loan`, configurations, `LibraryDbContext` e migration `InitialCreate` aplicada no PostgreSQL local (Docker, porta 5433). O resto da estrutura existe, mas está vazio.

---

## Leva 0 — Base de dados ✅
- [x] Models, enums e configurations
- [x] Migration `InitialCreate` (Users, Books, BookCopies, Loans)
- [x] PostgreSQL em Docker + User Secrets
- [x] Commit

## Leva 1 — Esqueleto do site
Objetivo: abrir `/` sem erro e ter um layout comum.
- [ ] `Views/_ViewStart.cshtml`, `Views/_ViewImports.cshtml`, `Views/Shared/_Layout.cshtml` (Bootstrap via CDN, menu Livros / Empréstimos / Entrar)
- [ ] `HomeController` com a action `Error` e a view `Views/Shared/Error.cshtml`, que o `Program.cs` já referencia
- [ ] Corrigir `BooksController`: `return View(books)` em vez de `/Views/Account/Books/...`
- [ ] `docker-compose.yml` com o PostgreSQL (porta 5433, volume `library-data`)
- [ ] Atualizar o README (4 tabelas, porta 5433, Docker)

**Pronto quando:** `dotnet run` abre a lista de livros com o layout.

## Leva 2 — Catálogo de livros (CRUD)
Objetivo: cadastrar e editar livros de verdade no banco.
- [ ] DTOs `CreateBookDto` e `UpdateBookDto` (renomear `UpdateBook.cs`), com `[Required]`, `[StringLength]` e ISBN de 13 dígitos
- [ ] `IBookService` / `BookService` usando `LibraryDbContext` (listar, detalhar, criar, editar, desativar)
- [ ] Registrar no `Program.cs` com `AddScoped<IBookService, BookService>()`
- [ ] `BooksController`: Index, Details, Create, Edit (GET/POST) com views
- [ ] ISBN duplicado vira mensagem de validação, não erro 500
- [ ] Busca por título/autor/ISBN e paginação na Index

**Pronto quando:** dá para cadastrar, editar e ver livros, e eles continuam lá depois de reiniciar a aplicação.

## Leva 3 — Exemplares (BookCopies)
Objetivo: cada livro mostra seus exemplares e quantos estão disponíveis.
- [ ] Na página Details do livro: lista de exemplares (código e status) e formulário "Adicionar exemplar"
- [ ] Mudar o status para `Lost` ou `Inactive` (nunca apagar)
- [ ] Na Index: coluna "Disponíveis X de Y"

**Pronto quando:** um livro com 3 exemplares mostra "3 de 3 disponíveis".

## Leva 4 — Contas e login
Objetivo: usuário se cadastra, entra e sai; só o bibliotecário altera o acervo.
- [ ] DTOs `RegisterUserDto` e `LoginDto`
- [ ] `IAuthService` / `AuthService` com `PasswordHasher<User>` e e-mail salvo em minúsculas
- [ ] Cookie auth no `Program.cs` (`AddAuthentication().AddCookie()` + `UseAuthentication()` antes de `UseAuthorization()`)
- [ ] `AccountController`: Register, Login, Logout e AccessDenied
- [ ] `[Authorize(Roles = "Librarian")]` em Create e Edit de livros e exemplares
- [ ] Seed do primeiro bibliotecário, com a senha vinda dos User Secrets (nunca no código)

**Pronto quando:** um usuário comum vê o catálogo mas recebe "acesso negado" ao tentar cadastrar um livro.

## Leva 5 — Empréstimos e devoluções
Objetivo: o fluxo principal da biblioteca, com as regras de negócio.
- [ ] DTOs `CreateLoanDto` e `LoanResponseDto`
- [ ] `ILoanService` / `LoanService`, com as regras abaixo:
  - só empresta exemplar `Available`
  - máximo de 3 empréstimos abertos por usuário
  - bloqueia quem tem empréstimo atrasado
  - prazo de 14 dias (`DueDate = hoje + 14`)
  - empréstimo e mudança de status do exemplar na mesma transação
  - devolução preenche `ReturnedAt` e volta o exemplar para `Available`, e devolver duas vezes é erro
- [ ] Limites (3 e 14) no `appsettings.json`, seção `LoanRules`
- [ ] `LoansController`: "Meus empréstimos" (User) e Emprestar, Devolver e Atrasados (Librarian)
- [ ] Tratar a violação do índice `IX_Loans_BookCopyId_Open` como "exemplar já emprestado"

**Pronto quando:** dá para emprestar, ver o atraso e devolver, e as regras barram os casos inválidos.

## Leva 6 — Testes
- [ ] Projeto `Library.Tests` (xUnit)
- [ ] Testes do `LoanService` com PostgreSQL real (Testcontainers) ou um banco de teste
- [ ] Casos: limite de 3, atraso bloqueia, exemplar já emprestado, devolução dupla

## Leva 7 — Reset de senha
- [ ] `ResetPasswordDto` + token com expiração (tabela `PasswordResetTokens` ou Data Protection)
- [ ] Envio de e-mail via serviço externo (`EmailApiKey` nos User Secrets / Key Vault)

## Leva 8 — Azure
- [ ] Key Vault no `Program.cs` fora de Development (`Azure.Extensions.AspNetCore.Configuration.Secrets` + `Azure.Identity`)
- [ ] App Service com Managed Identity + PostgreSQL Flexible Server
- [ ] Migrations via `dotnet ef migrations script --idempotent` no pipeline
- [ ] README de produção atualizado (sem senha nas App Settings)

---

## Regras para todas as levas
- Nunca editar `Migrations/` à mão: mudar model/configuration e rodar `dotnet ef migrations add NomeDaMudanca`.
- Controllers finos: regra de negócio fica nos services.
- Cada leva termina com `dotnet build` limpo e um commit `leva N: descrição`.
