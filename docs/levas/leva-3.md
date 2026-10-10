# Leva 3 — Exemplares (BookCopies) ✅

**Concluída em:** 2026-10-10 · **Branch:** `leva-3` (PR para `main`)

## O que foi feito
- `DTOs/BookCopies/CreateBookCopyDto.cs`: `BookId` + `Code` (obrigatório, até 30 caracteres), com mensagens em português.
- `Services/Interfaces/IBookCopyService.cs` + `Services/BookCopyService.cs`: `CreateAsync`, `SetLostAsync` (perdido ↔ disponível), `SetActiveAsync` (ativar/desativar) e `DeleteAsync` (soft delete).
- `Controllers/BookCopiesController.cs` fino: `Create`, `MarkLost`, `MarkAvailable`, `Deactivate`, `Reactivate`, `Delete` (todos POST com `[ValidateAntiForgeryToken]`). Toda ação volta para `Books/Details/{bookId}` com mensagem no `TempData`.
- `Program.cs`: `AddScoped<IBookCopyService, BookCopyService>()`.
- `Models/BookIndexViewModel.cs`: nova classe `BookListItem` (livro + `AvailableCopies` + `TotalCopies`); `BookService.SearchAsync` passou a projetar essas contagens.
- `Views/Books/Details.cshtml`: resumo "X de Y disponíveis", tabela de exemplares (código, status com selo, selo "Inativo") com as ações por exemplar, e o formulário "Adicionar exemplar".
- `Views/Books/Index.cshtml`: nova coluna **Disponíveis** ("X de Y" ou "Sem exemplares").

**Sem migration:** a tabela `BookCopies` (com `Code`, `Status`, `IsActive`, `DeletedAt` e o índice único parcial `IX_BookCopies_Code`) já existe desde a Leva 2. Esta leva é só código e telas.

## Decisões
- **"Disponíveis X de Y":** `Y` = total de exemplares não excluídos do livro; `X` = exemplares `Available` **e** ativos. Exemplar perdido, emprestado ou inativo não conta como disponível. As contagens respeitam o filtro global (`DeletedAt == null`).
- **Código duplicado:** mesmo tratamento do ISBN — o service checa antes de salvar ("Já existe um exemplar com este código.") e, se duas gravações chegarem juntas, captura o `23505` do índice único e devolve a mesma mensagem, nunca um 500. Como o índice é parcial (`WHERE "DeletedAt" IS NULL`), o código de um exemplar excluído pode ser recadastrado.
- **Status:** o usuário só alterna entre **Disponível** e **Perdido**. `Loaned` é controlado pelos empréstimos (Leva 5): exemplar emprestado não muda de status nem é desativado por estas telas.
- **Desativar antes de excluir** (igual aos livros): excluir exige o exemplar inativo; exemplar com empréstimos no histórico nunca é excluído (checado com `IgnoreQueryFilters()`). A exclusão é soft delete (preenche `DeletedAt`).
- **Reverter engano:** marcar como perdido e desativar têm volta (Disponível / Reativar).
- **Sem autorização ainda:** qualquer pessoa gerencia exemplares; o `[Authorize(Roles = "Librarian")]` entra na Leva 4.

## Validação
Feita contra um PostgreSQL 16 com as migrations `InitialCreate` e `AddActiveAndSoftDelete` aplicadas e o app rodando (`dotnet run --launch-profile http`). `dotnet build`: sem erros e sem avisos. Fluxo testado por HTTP + conferência no banco (`psql`):
- Livro novo com 3 exemplares mostra **"3 de 3 disponíveis"** nos detalhes e **"3 de 3"** na Index.
- Código duplicado volta com a mensagem certa (sem 500).
- Marcar perdido → "2 de 3" e `Status='Lost'` no banco; marcar disponível → volta a "3 de 3".
- Desativar → "2 de 3" e `IsActive=false`; excluir exemplar ativo é barrado ("Desative o exemplar antes de excluí-lo."); excluir exemplar inativo preenche `DeletedAt` (a linha continua no banco) e a contagem cai para "2 de 2".
- Reativar funciona; o código de um exemplar excluído pôde ser recadastrado.

## Pendências / observações
- Texto acentuado sai HTML-encoded (`&#xE1;`), padrão do Razor — aparece normal no navegador.
- Links "Empréstimos" e "Entrar" seguem dando 404 (Levas 5 e 4).
- Exemplar `Loaned` só vai existir a partir da Leva 5; as regras que o protegem (não mudar status, não desativar) já estão no service.

## Ponto de partida da Leva 4 (Contas e login)
- DTOs `RegisterUserDto` e `LoginDto`; `IAuthService`/`AuthService` com `PasswordHasher<User>` e e-mail em minúsculas.
- Cookie auth no `Program.cs` (`UseAuthentication()` antes de `UseAuthorization()`).
- `AccountController`: Register, Login, Logout, AccessDenied.
- `[Authorize(Roles = "Librarian")]` em Create/Edit de livros **e** nas ações de exemplares (`BookCopiesController`).
- Seed do primeiro bibliotecário com senha vinda dos User Secrets (nunca no código).

## Como testar
```bash
git pull
docker start library-db
dotnet ef database update   # não há migration nova nesta leva, mas o comando é inofensivo
dotnet run                  # http://localhost:5080
```
Abra um livro, adicione exemplares, marque como perdido/disponível, desative/reative e exclua um inativo. A lista de livros mostra "Disponíveis X de Y".
