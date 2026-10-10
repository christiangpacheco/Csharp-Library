# Leva 2 — Catálogo de livros (CRUD) ✅

**Concluída em:** 2026-10-09 · **Branch:** `leva-2` (PR para `main`)

## O que foi feito
- `DTOs/Books/CreateBookDto.cs` e `UpdateBookDto.cs` (renomeado de `UpdateBook.cs`; herda de `CreateBookDto` e adiciona `Id`). Validações com mensagens em português: título até 300, autor até 200, ISBN obrigatório e data de publicação obrigatória.
- `Services/Interfaces/IBookService.cs` + `Services/BookService.cs`: `SearchAsync` (busca + paginação + inativos), `GetByIdAsync` (inclui exemplares), `CreateAsync`, `UpdateAsync`, `SetActiveAsync`, `DeleteAsync`.
- `Models/ISoftDeletable.cs` + `DeletedAt` nas 4 entidades e `IsActive` em `User`, `Book` e `BookCopy`; filtros globais nas configurations.
- `Services/ServiceResult.cs`: retorno dos services (sucesso + `Id`, ou erro ligado a um campo do formulário).
- `Models/BookIndexViewModel.cs`: lista paginada para a Index.
- `Program.cs`: `AddScoped<IBookService, BookService>()`.
- `BooksController` fino, só chamando o service: Index, Details, Create, Edit, Deactivate, Reactivate e Delete (POSTs com `[ValidateAntiForgeryToken]`).
- Views: `Index` (busca, tabela com link para os detalhes, paginação), `Details`, `Create`, `Edit`, `Delete` e o formulário compartilhado `_BookForm`.
- `Views/Shared/_Alerts.cshtml` (mensagens de sucesso/erro via `TempData`, incluído no layout) e `_ValidationScriptsPartial.cshtml` (validação no navegador via CDN).

## Decisões
- **ISBN normalizado:** o formulário aceita `978-0-13-235088-4` ou com espaços, e o service guarda só os 13 dígitos. Fora disso, o erro aparece no campo.
- **ISBN duplicado:** o service checa antes de salvar e devolve a mensagem "Já existe um livro com este ISBN.". Se duas gravações chegarem ao mesmo tempo, o índice único `IX_Books_ISBN` barra a segunda, e o erro do PostgreSQL (`23505`) vira a mesma mensagem, nunca um 500.
- **Ativo/inativo e soft delete em todas as tabelas** (pedido do Christian durante a leva), migration `AddActiveAndSoftDelete`:
  - `DeletedAt` em Users, Books, BookCopies e Loans. `Remove()` vira um UPDATE que preenche `DeletedAt` (no `SaveChanges` do `LibraryDbContext`), e um filtro global esconde os excluídos de todas as consultas. Para ver o histórico, usar `IgnoreQueryFilters()`.
  - `IsActive` (padrão `true`) em Users, Books e BookCopies. Desativar e Reativar são ações próprias; só registro inativo pode ser excluído. Loans não tem `IsActive`, porque o estado do empréstimo é aberto ou devolvido.
  - `BookCopyStatus` perdeu `Inactive` (agora `Available`, `Loaned`, `Lost`), porque o `IsActive` cobre esse caso.
  - Índices únicos (`Email`, `ISBN`, `Code`) e o de empréstimo aberto passaram a ser parciais (`WHERE "DeletedAt" IS NULL`): um ISBN de livro excluído pode ser recadastrado; o de livro inativo continua ocupado.
  - `IsActive` usa `HasDefaultValue(true).HasSentinel(true)`: o EF só envia a coluna quando ela é `false`, e as linhas antigas ficaram ativas.
- **Livros:** inativo some da lista (há a opção "Mostrar inativos", com selo "Inativo"). Excluir exige o livro inativo e sem exemplares.
- **Busca:** `ILIKE` em título e autor, sem diferenciar maiúsculas; no ISBN busca pelos dígitos digitados. Ordenação por título, 10 livros por página, página fora do intervalo cai na última.
- **Sem autorização ainda:** qualquer pessoa cadastra e edita. O `[Authorize(Roles = "Librarian")]` entra na Leva 4.

## Validação
Feita contra um PostgreSQL 16 com as migrations `InitialCreate` e `AddActiveAndSoftDelete` aplicadas e o app rodando com `dotnet run --launch-profile http`:
- `dotnet build`: sem erros e sem avisos.
- Criar livro redireciona para os detalhes; editar salva; os dados continuam lá depois de reiniciar o app.
- ISBN duplicado (no cadastro e na edição), ISBN com 3 ou 17 dígitos e campos vazios voltam ao formulário com a mensagem certa.
- Livro ativo não é excluído; desativar tira da lista, "Mostrar inativos" traz de volta com selo, reativar funciona.
- Excluir livro inativo preenche `DeletedAt` (a linha continua no banco), os detalhes passam a dar 404 e o ISBN pode ser recadastrado. Livro inativo com exemplar não é excluído.
- No banco: 4 colunas `DeletedAt`, 3 colunas `IsActive` com default `true`, CHECK de status sem `Inactive`, livros antigos ficaram ativos.

## Revisão e teste de segurança
Dois agentes revisaram o PR antes do merge:
- **Estrutura e lógica:** sem bloqueante. Apliquei três ajustes de robustez: o soft delete agora grava só `DeletedAt` (não regrava as outras colunas lidas no `FindAsync`); a busca escapa os curingas `%` e `_` do `ILIKE` (antes, buscar `_` trazia tudo); e o `foreach` do `SaveChanges` percorre uma cópia (`ToList()`).
- **SQL injection (caixa-preta, sem contexto):** nenhum indício. Aspas, `;`, `--`, `/* */`, nomes de tabela e `OR 1=1`/`UNION` foram gravados literais no cadastro e tratados como texto na busca; nenhum erro 500, nenhum dado afetado. Consistente com consultas parametrizadas (EF Core).

**Observações para as próximas levas:**
- A migration `AddActiveAndSoftDelete` recria o CHECK de `BookCopyStatus` sem `Inactive` e supõe que não exista exemplar com `Status='Inactive'` (verdadeiro agora: a Leva 3 ainda não começou e não há exemplares). Num banco que já tivesse esse valor, o `database update` falharia no CHECK.
- O soft delete também atualiza o `UpdatedAt` de Books e Users.
- Filtros globais em principais de navegações obrigatórias (`Loan.BookCopy`, `Loan.User`) viram INNER JOIN: nas telas de histórico da Leva 5, usar `IgnoreQueryFilters()` para não esconder o registro de um item excluído.
- Busca por "CLEAN", por "ana souza" e por "978-0-13" acha os livros certos; com 12 resultados, a página 1 mostra 10, a página 2 mostra 2, e os links mantêm o filtro.
- `/Books/Details/999` e `/Books/Edit/999` dão 404; POST sem token antiforgery dá 400.

## Como testar
```bash
git pull
docker start library-db
dotnet ef database update
dotnet run            # http://localhost:5080
```
Há uma migration nova (`AddActiveAndSoftDelete`): rode `dotnet ef database update` depois do `git pull`.

## Pendências / observações
- O texto acentuado sai codificado no HTML (`&#xE1;`), o que é o padrão do Razor e não aparece para quem usa o navegador.
- Links "Empréstimos" e "Entrar" seguem dando 404 (Levas 5 e 4).

## Ponto de partida da Leva 3
- Nos detalhes do livro: listar exemplares (código e status) e formulário "Adicionar exemplar".
- Marcar exemplar como `Lost`; Desativar/Reativar (`IsActive`); excluir só exemplar inativo. Exemplar inativo não pode ser emprestado (regra da Leva 5).
- Na Index: coluna "Disponíveis X de Y" (contagem por `BookCopyStatus.Available`).
- Código do exemplar é único (`IX_BookCopies_Code`): tratar duplicado do mesmo jeito que o ISBN (checagem + captura do `23505`).
