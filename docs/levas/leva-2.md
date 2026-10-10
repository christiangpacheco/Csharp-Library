# Leva 2 — Catálogo de livros (CRUD) ✅

**Concluída em:** 2026-10-09 · **Branch:** `leva-2` (PR para `main`)

## O que foi feito
- `DTOs/Books/CreateBookDto.cs` e `UpdateBookDto.cs` (renomeado de `UpdateBook.cs`; herda de `CreateBookDto` e adiciona `Id`). Validações com mensagens em português: título até 300, autor até 200, ISBN obrigatório e data de publicação obrigatória.
- `Services/Interfaces/IBookService.cs` + `Services/BookService.cs`: `SearchAsync` (busca + paginação), `GetByIdAsync` (inclui exemplares), `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
- `Services/ServiceResult.cs`: retorno dos services (sucesso + `Id`, ou erro ligado a um campo do formulário).
- `Models/BookIndexViewModel.cs`: lista paginada para a Index.
- `Program.cs`: `AddScoped<IBookService, BookService>()`.
- `BooksController` fino, só chamando o service: Index, Details, Create, Edit e Delete (GET + POST com `[ValidateAntiForgeryToken]`).
- Views: `Index` (busca, tabela com link para os detalhes, paginação), `Details`, `Create`, `Edit`, `Delete` e o formulário compartilhado `_BookForm`.
- `Views/Shared/_Alerts.cshtml` (mensagens de sucesso/erro via `TempData`, incluído no layout) e `_ValidationScriptsPartial.cshtml` (validação no navegador via CDN).

## Decisões
- **ISBN normalizado:** o formulário aceita `978-0-13-235088-4` ou com espaços, e o service guarda só os 13 dígitos. Fora disso, o erro aparece no campo.
- **ISBN duplicado:** o service checa antes de salvar e devolve a mensagem "Já existe um livro com este ISBN.". Se duas gravações chegarem ao mesmo tempo, o índice único `IX_Books_ISBN` barra a segunda, e o erro do PostgreSQL (`23505`) vira a mesma mensagem, nunca um 500.
- **"Desativar" virou "excluir sem exemplares":** a tabela `Books` não tem coluna de ativo/inativo, e criar uma exigiria migration. Livro sem exemplares pode ser excluído; com exemplares, a exclusão é bloqueada com aviso (a FK é `Restrict`). Quem "desativa" é o exemplar, na Leva 3.
- **Busca:** `ILIKE` em título e autor, sem diferenciar maiúsculas; no ISBN busca pelos dígitos digitados. Ordenação por título, 10 livros por página, página fora do intervalo cai na última.
- **Sem autorização ainda:** qualquer pessoa cadastra e edita. O `[Authorize(Roles = "Librarian")]` entra na Leva 4.

## Validação
Feita contra um PostgreSQL 16 com a migration `InitialCreate` aplicada e o app rodando com `dotnet run --launch-profile http`:
- `dotnet build`: sem erros e sem avisos.
- Criar livro redireciona para os detalhes; editar salva; os dados continuam lá depois de reiniciar o app.
- ISBN duplicado (no cadastro e na edição), ISBN com 3 ou 17 dígitos e campos vazios voltam ao formulário com a mensagem certa.
- Livro com exemplar não é excluído (aviso nos detalhes e botão desabilitado); livro sem exemplar é excluído.
- Busca por "CLEAN", por "ana souza" e por "978-0-13" acha os livros certos; com 12 resultados, a página 1 mostra 10, a página 2 mostra 2, e os links mantêm o filtro.
- `/Books/Details/999` e `/Books/Edit/999` dão 404; POST sem token antiforgery dá 400.

## Como testar
```bash
git pull
docker start library-db
dotnet run            # http://localhost:5080
```
Não há migration nova nesta leva.

## Pendências / observações
- O texto acentuado sai codificado no HTML (`&#xE1;`), o que é o padrão do Razor e não aparece para quem usa o navegador.
- Links "Empréstimos" e "Entrar" seguem dando 404 (Levas 5 e 4).

## Ponto de partida da Leva 3
- Nos detalhes do livro: listar exemplares (código e status) e formulário "Adicionar exemplar".
- Mudar status para `Lost` ou `Inactive`, nunca apagar.
- Na Index: coluna "Disponíveis X de Y" (contagem por `BookCopyStatus.Available`).
- Código do exemplar é único (`IX_BookCopies_Code`): tratar duplicado do mesmo jeito que o ISBN (checagem + captura do `23505`).
