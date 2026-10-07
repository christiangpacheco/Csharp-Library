# Plano de desenvolvimento em levas

Cada leva é pequena o bastante para caber num commit (ou PR) e termina com o projeto compilando e rodando. Marque os itens conforme forem concluídos.

## Estado atual

- [x] Models: `User`, `Book`, `BookCopy`, `Loan`, enums `UserRole` e `BookCopyStatus`
- [x] Configurations (constraints, índices, `DeleteBehavior.Restrict`) e `LibraryDbContext`
- [x] Migration `20261007005456_InitialCreate` aplicada no PostgreSQL (container `library-db`, porta 5433)
- [ ] Controllers, services, DTOs e views (arquivos existem, mas estão vazios)

Critério de pronto para toda leva: `dotnet build` sem erros, `dotnet ef migrations has-pending-model-changes` sem mudanças e o fluxo da leva testado no navegador.

---

## Leva 1 — Base do MVC

Objetivo: a aplicação abre no navegador com layout e a listagem de livros vinda do banco.

- [ ] `Views/_ViewStart.cshtml` e `Views/_ViewImports.cshtml` (`@using Library.Models`, `@using Library.DTOs.*`, tag helpers)
- [ ] `Views/Shared/_Layout.cshtml` com menu (Livros, Empréstimos, Entrar/Sair)
- [ ] `HomeController` + `Views/Home/Error.cshtml` (o `Program.cs` já aponta para `/Home/Error`)
- [ ] Corrigir `BooksController.Index`: hoje retorna `/Views/Account/Books/Index.cshtml`, que não existe; usar `return View(books)`
- [ ] `BooksController` lendo do `LibraryDbContext` em vez da lista fixa

## Leva 2 — Livros (CRUD)

Objetivo: cadastrar, editar, listar, ver detalhes e remover livros.

- [ ] DTOs: `CreateBookDto`, `UpdateBookDto` (renomear `UpdateBook.cs`) com Data Annotations (`[Required]`, `[StringLength]`, ISBN com 13 dígitos)
- [ ] `IBookService` / `BookService`: `ListAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- [ ] Tratar ISBN duplicado (índice único) com mensagem amigável no `ModelState`
- [ ] Bloquear exclusão de livro com exemplares (FK `Restrict`) com mensagem amigável
- [ ] Registrar o service no `Program.cs` (`AddScoped<IBookService, BookService>()`)
- [ ] Views: `Index`, `Details`, `Create`, `Edit`, `Delete`

## Leva 3 — Exemplares (BookCopy)

Objetivo: cada livro tem exemplares físicos com código e status.

- [ ] Adicionar exemplar a um livro (código único) a partir da tela de detalhes
- [ ] Listar exemplares com status (`Available`, `Loaned`, `Lost`, `Inactive`)
- [ ] Alterar status para `Lost` / `Inactive` (desativar em vez de apagar)
- [ ] Mostrar na listagem de livros a contagem de exemplares disponíveis

## Leva 4 — Autenticação e papéis

Objetivo: usuários se cadastram e entram; só bibliotecários gerenciam o acervo.

- [ ] DTOs: `RegisterUserDto`, `LoginDto`, `ResetPasswordDto`
- [ ] `IAuthService` / `AuthService` com `PasswordHasher<User>` (nunca salvar senha em texto)
- [ ] Autenticação por cookie no `Program.cs`: `AddAuthentication().AddCookie(...)` e `app.UseAuthentication()` antes de `UseAuthorization()`
- [ ] Claims: `NameIdentifier`, `Name`, `Email`, `Role`
- [ ] `AccountController`: `Register`, `Login`, `Logout`, `AccessDenied`
- [ ] `[Authorize(Roles = "Librarian")]` nas ações de criação/edição/remoção de livros e exemplares
- [ ] Seed de um bibliotecário inicial (credenciais em User Secrets, não no repositório)

## Leva 5 — Empréstimos

Objetivo: emprestar e devolver exemplares respeitando as regras de negócio.

- [ ] DTOs: `CreateLoanDto`, `LoanResponseDto`
- [ ] `ILoanService` / `LoanService`:
  - [ ] Emprestar só exemplar `Available`; mudar para `Loaned` na mesma transação
  - [ ] `DueDate` padrão (ex.: 14 dias) e validação `DueDate >= data do empréstimo`
  - [ ] Devolver: preencher `ReturnedAt` e voltar o exemplar para `Available`
  - [ ] Tratar violação do índice `IX_Loans_BookCopyId_Open` (dois empréstimos abertos)
- [ ] `LoansController` e views: empréstimos em aberto, atrasados, histórico
- [ ] Usuário comum vê só os próprios empréstimos; bibliotecário vê todos

## Leva 6 — Qualidade e acabamento

- [ ] Projeto de testes (xUnit) para `BookService` e `LoanService` (regras de empréstimo/devolução)
- [ ] Paginação e busca por título/autor/ISBN na listagem de livros
- [ ] Mensagens de feedback (`TempData`) após criar/editar/excluir
- [ ] Atualizar o `README.md` (subir o Postgres com Docker, aplicar migrations, criar o bibliotecário)
- [ ] `docker-compose.yml` com o Postgres (opcional)
