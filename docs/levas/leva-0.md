# Leva 0 — Base de dados ✅

**Concluída em:** 2026-10-07 · **Commit:** `3d1a9a7` (plano: `ca73759`, `6d276a8`)

## O que foi feito
- Models: `User`, `Book`, `BookCopy`, `Loan` e enums `UserRole` (`User`, `Librarian`) e `BookCopyStatus` (`Available`, `Loaned`, `Lost`, `Inactive`). _Na Leva 2, `Inactive` foi substituído pelo campo `IsActive`._
- `Loan` aponta para `BookCopy` (exemplar físico), não para `Book`. `Book.PublishedAt` e `Loan.DueDate` são `DateOnly` (coluna `date`).
- Configurations em `Data/Configurations/` (aplicadas por `ApplyConfigurationsFromAssembly`):
  - Índices únicos: `Users.Email`, `Books.ISBN`, `BookCopies.Code`.
  - CHECKs: ISBN com 13 dígitos, `Role` e `Status` só com valores do enum (salvos como texto), `DueDate >= data do empréstimo`, `ReturnedAt >= BorrowedAt`.
  - Índice único filtrado `IX_Loans_BookCopyId_Open` (`ReturnedAt IS NULL`): um exemplar só tem um empréstimo aberto.
  - Todas as FKs com `DeleteBehavior.Restrict` (preserva histórico).
  - `CreatedAt`/`UpdatedAt` com default `now()`; `LibraryDbContext.SaveChanges` preenche `UpdatedAt`.
- Migration `20261007005456_InitialCreate` (refeita do zero; a antiga `20260821003930`, editada à mão, foi removida) aplicada no banco.

## Ambiente
- PostgreSQL 17 no container Docker `library-db`, porta `5433`, banco `library`, usuário `admin`.
- Connection string nos User Secrets (`ConnectionStrings:DefaultConnection`).
- `dotnet-ef` 10.0.11 em `~/.dotnet/tools` (adicionar ao `PATH` se necessário).

## Pendências / observações
- O usuário `christian` não está no grupo `docker`: `docker` sem `sudo` falha. Correção: `sudo usermod -aG docker $USER && newgrp docker`.
- Arquivos de Controllers, Services e DTOs existiam vazios (serão preenchidos nas próximas levas).
