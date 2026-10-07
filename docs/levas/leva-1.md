# Leva 1 — Esqueleto do site ✅

**Concluída em:** 2026-10-07 · **Commits:** `85bdc46`, `578af64`

## O que foi feito
- `Views/_ViewStart.cshtml`, `Views/_ViewImports.cshtml` (`@using Library`, `@using Library.Models`, tag helpers).
- `Views/Shared/_Layout.cshtml`: Bootstrap 5.3 via CDN (jsDelivr), navbar com Livros, Empréstimos e Entrar.
- `HomeController.Error` + `Views/Shared/Error.cshtml` + `Models/ErrorViewModel.cs`.
- `BooksController` recebe `LibraryDbContext` por injeção e lista `Books` (`AsNoTracking`, ordenado por título) com `return View(books)`.
- `Views/Books/Index.cshtml`: tabela (Título, Autor, ISBN, Publicação) e alerta "Nenhum livro cadastrado ainda." quando vazia.
- `docker-compose.yml`: `postgres:17`, `container_name: library-db`, porta `5433:5432`, volume `library-data`, senha via `${POSTGRES_PASSWORD}` do `.env`.
- `.env.example` versionado (`.gitignore` ignora `.env` e `.env.*`, com exceção `!.env.example`).
- README: 4 tabelas, porta 5433, Docker/compose, aviso para não editar `Migrations/` à mão.

## Decisão fora do plano
- Criado `Properties/launchSettings.json` (perfis `http` em `http://localhost:5080` e `https` em `https://localhost:7080`, ambos `ASPNETCORE_ENVIRONMENT=Development`). Sem ele o `dotnet run` subia em Production, os User Secrets não eram carregados e toda página com banco dava 500 ("ConnectionString property has not been initialized").

## Como rodar
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
docker start library-db            # ou: cp .env.example .env && docker compose up -d
dotnet user-secrets list           # conferir ConnectionStrings:DefaultConnection (porta 5433)
dotnet ef database update
dotnet run                         # http://localhost:5080
```

## Validação
- `dotnet build`: sem erros e sem avisos.
- `dotnet run --launch-profile http` + `curl`: `/` → 200, `/Books` → 200 (layout + "Nenhum livro cadastrado ainda."), `/Home/Error` → 200.
- `docker compose config`: válido com `POSTGRES_PASSWORD` definido. **Não** foi rodado `docker compose up` porque o `library-db` atual (criado com `docker run`) usa o mesmo nome e porta.

## Pendências / observações
- Links "Empréstimos" (`/Loans`) e "Entrar" (`/Account/Login`) ainda dão 404 — chegam nas Levas 5 e 4.
- Sem `wwwroot/`; CSS/JS vêm do CDN. `UseStaticFiles` continua no `Program.cs` para uso futuro.
- `app.UseHttpsRedirection()` loga "Failed to determine the https port" no perfil `http`; inofensivo. Use o perfil `https` se quiser HTTPS.

## Ponto de partida da Leva 2
- Arquivos vazios a preencher: `DTOs/Books/CreateBookDto.cs`, `DTOs/Books/UpdateBook.cs` (renomear para `UpdateBookDto.cs`), `Services/Interfaces/IBookService.cs`, `Services/BookService.cs`.
- Registrar `AddScoped<IBookService, BookService>()` no `Program.cs`.
- Mover a consulta do `BooksController` para o `BookService` e adicionar Details, Create, Edit, busca e paginação.
- ISBN duplicado deve virar erro de validação (índice único `IX_Books_ISBN`), não 500.
