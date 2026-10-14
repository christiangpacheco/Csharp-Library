# Leva 5 — Empréstimos e devoluções ✅

**Concluída em:** 2026-10-14 · **Branch:** `leva-5` (PR para `main`)

## O que foi feito
- `appsettings.json`: seção `LoanRules` (`MaxOpenLoansPerUser` = 3, `LoanPeriodDays` = 14), ligada por `Services/LoanRules.cs` + `Configure<LoanRules>` no `Program.cs`.
- `DTOs/Loans/CreateLoanDto.cs` (BookCopyId + UserId) e `LoanResponseDto.cs` (modelo de leitura com `IsReturned`/`IsOverdue`).
- `Services/Interfaces/ILoanService.cs` + `Services/LoanService.cs`: `BorrowAsync`, `ReturnAsync`, `GetUserLoansAsync`, `GetOpenLoansAsync`, `GetAvailableCopyAsync`, `GetBorrowersAsync`.
- `Controllers/LoansController.cs`: `Index` ("Meus empréstimos", qualquer usuário logado), `Open` (gestão — abertos, vencidos primeiro), `Create` GET/POST (Emprestar, bibliotecário) e `Return` (Devolver, bibliotecário). POSTs com `[ValidateAntiForgeryToken]`.
- Views `Views/Loans/Index.cshtml`, `Open.cshtml` e `Create.cshtml`.
- `Views/Shared/_Layout.cshtml`: menu "Meus empréstimos" (logado) e "Empréstimos (gestão)" (bibliotecário).
- `Views/Books/Details.cshtml`: botão **Emprestar** em cada exemplar ativo e disponível (bibliotecário).
- **Ajuste do "X de Y"** (pendência da Leva 4): em `BookService.SearchAsync`, o total passou a contar só exemplares **ativos** (`Count(c => c.IsActive)`); exemplar inativo fica de fora dos dois números. A tela de detalhes já filtrava certo.

**Sem migration:** a tabela `Loans` (UserId, BookCopyId, BorrowedAt, DueDate, ReturnedAt, DeletedAt + índice único parcial `IX_Loans_BookCopyId_Open`) já existe desde a Leva 2.

## Regras de negócio (no LoanService)
- **Só empresta exemplar disponível:** ativo e `Status == Available`.
- **Limite de 3 abertos por leitor:** conta empréstimos com `ReturnedAt == null`.
- **Bloqueio por atraso:** leitor com qualquer empréstimo aberto e `DueDate < hoje` não pega outro.
- **Prazo de 14 dias:** `DueDate = data de hoje (UTC) + LoanPeriodDays`.
- **Mesma transação:** criar o empréstimo e mudar o exemplar para `Loaned` saem num único `SaveChanges`.
- **Devolução:** preenche `ReturnedAt` e volta o exemplar para `Available` (se estava `Loaned`); devolver duas vezes é erro.
- **Corrida:** se dois empréstimos abertos do mesmo exemplar forem tentados juntos, o índice único parcial `IX_Loans_BookCopyId_Open` barra o segundo e o erro vira "Este exemplar já está emprestado." (captura do `23505`), nunca um 500.

## Decisões
- **Emprestar é do bibliotecário:** ele escolhe o leitor (lista de usuários ativos) para um exemplar disponível, a partir da página do livro. O leitor só vê os seus empréstimos.
- **Datas:** `BorrowedAt` é gravado em UTC no código (para o `DueDate` casar com o CHECK `CK_Loans_DueDate`); as telas mostram em horário local.
- **Histórico preservado:** as consultas de empréstimo usam `Include` de exemplar/livro/usuário; como exemplar ou usuário com empréstimo não pode ser excluído (FK Restrict + checagem no serviço), o histórico nunca some.
- **Exemplar perdido na devolução:** se o exemplar foi marcado como perdido enquanto emprestado, a devolução não o força para `Available` (continua `Lost`).

## Validação
`dotnet build` sem erros nem avisos. App rodando contra um PostgreSQL real, com bibliotecário semeado e dois leitores. Fluxo testado por HTTP + conferência no banco (14/14):
- Emprestar: exemplar vira `Loaned`, `DueDate` = hoje + 14, aparece em "Meus empréstimos" do leitor.
- Emprestar o mesmo exemplar de novo: indisponível (a tela de empréstimo redireciona).
- 3 empréstimos abertos OK; o 4º é barrado com a mensagem do limite.
- Leitor com empréstimo atrasado é bloqueado de pegar outro.
- Devolver: exemplar volta a `Available`, `ReturnedAt` preenchido; devolver de novo dá "Este empréstimo já foi devolvido.".
- "X de Y": livro com 3 exemplares e 1 inativo mostra "2 de 2" (não "2 de 3").

## Pendências / observações
- Usuário desativado depois de logar continua com acesso até o cookie expirar (8h) — herdado da Leva 4.
- Não há tela para o próprio leitor pedir empréstimo (é sempre o bibliotecário que empresta), coerente com o PLANO.
- Sem notificação/e-mail de atraso (fora do escopo).

## Ponto de partida da Leva 6 (Testes)
- Projeto `Library.Tests` (xUnit).
- Testar o `LoanService` com PostgreSQL real (Testcontainers) ou banco de teste dedicado.
- Casos: limite de 3, atraso bloqueia, exemplar já emprestado (índice único), devolução dupla, prazo de 14 dias.

## Como testar
```bash
git pull
docker start library-db
dotnet ef database update   # sem migration nova nesta leva
dotnet run                  # http://localhost:5080
```
Entre como bibliotecário, abra um livro com exemplares disponíveis, clique em **Emprestar**, escolha um leitor. Veja "Empréstimos (gestão)" para devolver e para os atrasados; o leitor vê os seus em "Meus empréstimos".
