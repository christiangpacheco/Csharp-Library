using Library.Data;
using Library.Models;
using Library.Services;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests;

// Testes do LoanService contra um PostgreSQL real: as regras dependem de índice único parcial,
// CHECK constraints e enum em string, que um banco em memória não reproduz. Cada teste roda em
// um esquema limpo (TRUNCATE antes de cada um) e usa seu próprio DbContext, como em uma requisição.
public class LoanServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public LoanServiceTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await using var context = _fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE \"Loans\", \"BookCopies\", \"Books\", \"Users\" RESTART IDENTITY CASCADE;");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // --- Prazo de 14 dias -------------------------------------------------

    [Fact]
    public async Task BorrowAsync_ExemplarDisponivel_DefinePrazoDe14DiasEMarcaComoEmprestado()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerId = await SeedUser("leitor@test.local");

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context);

        var result = await service.BorrowAsync(copyIds[0], readerId);

        Assert.True(result.Succeeded, result.Error);

        await using var check = _fixture.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.BookCopyId == copyIds[0]);
        var expectedDueDate = DateOnly.FromDateTime(loan.BorrowedAt).AddDays(14);
        Assert.Equal(expectedDueDate, loan.DueDate);
        Assert.Null(loan.ReturnedAt);

        var copy = await check.BookCopies.SingleAsync(c => c.Id == copyIds[0]);
        Assert.Equal(BookCopyStatus.Loaned, copy.Status);
    }

    [Fact]
    public async Task BorrowAsync_PrazoConfiguravel_UsaLoanPeriodDays()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerId = await SeedUser("leitor@test.local");

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context, new LoanRules { LoanPeriodDays = 7 });

        var result = await service.BorrowAsync(copyIds[0], readerId);
        Assert.True(result.Succeeded, result.Error);

        await using var check = _fixture.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.BookCopyId == copyIds[0]);
        Assert.Equal(DateOnly.FromDateTime(loan.BorrowedAt).AddDays(7), loan.DueDate);
    }

    // --- Limite de 3 empréstimos abertos ----------------------------------

    [Fact]
    public async Task BorrowAsync_QuartoEmprestimoAberto_BarraPeloLimiteDeTres()
    {
        var (_, copyIds) = await SeedBookWithCopies(4);
        var readerId = await SeedUser("leitor@test.local");

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context);

        Assert.True((await service.BorrowAsync(copyIds[0], readerId)).Succeeded);
        Assert.True((await service.BorrowAsync(copyIds[1], readerId)).Succeeded);
        Assert.True((await service.BorrowAsync(copyIds[2], readerId)).Succeeded);

        var fourth = await service.BorrowAsync(copyIds[3], readerId);

        Assert.False(fourth.Succeeded);
        Assert.Contains("limite de 3", fourth.Error);

        await using var check = _fixture.CreateContext();
        Assert.Equal(3, await check.Loans.CountAsync(l => l.UserId == readerId && l.ReturnedAt == null));
        // O exemplar recusado continua disponível (a transação do 4º foi barrada antes de gravar).
        Assert.Equal(BookCopyStatus.Available, (await check.BookCopies.SingleAsync(c => c.Id == copyIds[3])).Status);
    }

    // --- Bloqueio por atraso ----------------------------------------------

    [Fact]
    public async Task BorrowAsync_LeitorComEmprestimoAtrasado_NaoPegaOutro()
    {
        var (_, copyIds) = await SeedBookWithCopies(2);
        var readerId = await SeedUser("leitor@test.local");
        await SeedOverdueLoan(copyIds[0], readerId);

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context);

        var result = await service.BorrowAsync(copyIds[1], readerId);

        Assert.False(result.Succeeded);
        Assert.Contains("atrasado", result.Error);

        await using var check = _fixture.CreateContext();
        Assert.Equal(BookCopyStatus.Available, (await check.BookCopies.SingleAsync(c => c.Id == copyIds[1])).Status);
    }

    // --- Exemplar já emprestado -------------------------------------------

    [Fact]
    public async Task BorrowAsync_ExemplarEmprestado_ImpedePeloStatusAntesDoBanco()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerA = await SeedUser("a@test.local");
        var readerB = await SeedUser("b@test.local");

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context);

        Assert.True((await service.BorrowAsync(copyIds[0], readerA)).Succeeded);

        var second = await service.BorrowAsync(copyIds[0], readerB);

        Assert.False(second.Succeeded);
        Assert.Contains("não está disponível", second.Error);
    }

    [Fact]
    public async Task BorrowAsync_CorridaNoMesmoExemplar_BarradaPeloIndiceUnico()
    {
        // Simula a corrida: já existe um empréstimo aberto para o exemplar, mas o status dele
        // continua Available (como se dois pedidos tivessem lido "disponível" ao mesmo tempo).
        // O serviço passa pela checagem de status e é o índice único parcial que barra o segundo.
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerA = await SeedUser("a@test.local");
        var readerB = await SeedUser("b@test.local");
        await SeedOpenLoanKeepingCopyAvailable(copyIds[0], readerA);

        await using var context = _fixture.CreateContext();
        var service = _fixture.CreateLoanService(context);

        var result = await service.BorrowAsync(copyIds[0], readerB);

        Assert.False(result.Succeeded);
        Assert.Equal("Este exemplar já está emprestado.", result.Error);

        await using var check = _fixture.CreateContext();
        // O segundo empréstimo não entrou: continua havendo só um aberto para o exemplar.
        Assert.Equal(1, await check.Loans.CountAsync(l => l.BookCopyId == copyIds[0] && l.ReturnedAt == null));
    }

    // --- Devolução e devolução dupla --------------------------------------

    [Fact]
    public async Task ReturnAsync_Emprestado_VoltaParaDisponivelEPreencheReturnedAt()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerId = await SeedUser("leitor@test.local");

        int loanId;
        await using (var context = _fixture.CreateContext())
        {
            var service = _fixture.CreateLoanService(context);
            loanId = (await service.BorrowAsync(copyIds[0], readerId)).Id!.Value;
        }

        await using (var context = _fixture.CreateContext())
        {
            var service = _fixture.CreateLoanService(context);
            var result = await service.ReturnAsync(loanId);
            Assert.True(result.Succeeded, result.Error);
        }

        await using var check = _fixture.CreateContext();
        var loan = await check.Loans.SingleAsync(l => l.Id == loanId);
        Assert.NotNull(loan.ReturnedAt);
        Assert.Equal(BookCopyStatus.Available, (await check.BookCopies.SingleAsync(c => c.Id == copyIds[0])).Status);
    }

    [Fact]
    public async Task ReturnAsync_Duas_Vezes_SegundaRetornaErro()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerId = await SeedUser("leitor@test.local");

        int loanId;
        await using (var context = _fixture.CreateContext())
        {
            var service = _fixture.CreateLoanService(context);
            loanId = (await service.BorrowAsync(copyIds[0], readerId)).Id!.Value;
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.True((await _fixture.CreateLoanService(context).ReturnAsync(loanId)).Succeeded);
        }

        await using (var context = _fixture.CreateContext())
        {
            var result = await _fixture.CreateLoanService(context).ReturnAsync(loanId);
            Assert.False(result.Succeeded);
            Assert.Contains("já foi devolvido", result.Error);
        }
    }

    [Fact]
    public async Task ReturnAsync_ExemplarPerdido_NaoForcaParaDisponivel()
    {
        var (_, copyIds) = await SeedBookWithCopies(1);
        var readerId = await SeedUser("leitor@test.local");

        int loanId;
        await using (var context = _fixture.CreateContext())
        {
            loanId = (await _fixture.CreateLoanService(context).BorrowAsync(copyIds[0], readerId)).Id!.Value;
        }

        // Exemplar é marcado como perdido enquanto estava emprestado.
        await using (var context = _fixture.CreateContext())
        {
            var copy = await context.BookCopies.SingleAsync(c => c.Id == copyIds[0]);
            copy.Status = BookCopyStatus.Lost;
            await context.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            Assert.True((await _fixture.CreateLoanService(context).ReturnAsync(loanId)).Succeeded);
        }

        await using var check = _fixture.CreateContext();
        Assert.Equal(BookCopyStatus.Lost, (await check.BookCopies.SingleAsync(c => c.Id == copyIds[0])).Status);
    }

    // --- Helpers ----------------------------------------------------------

    private async Task<(int bookId, int[] copyIds)> SeedBookWithCopies(int count)
    {
        await using var context = _fixture.CreateContext();
        var book = new Book
        {
            Title = "Livro de Teste",
            Author = "Autor",
            ISBN = "9780000000000",
            PublishedAt = new DateOnly(2020, 1, 1)
        };
        for (var i = 1; i <= count; i++)
        {
            book.Copies.Add(new BookCopy { Code = $"C{i}", Status = BookCopyStatus.Available });
        }

        context.Books.Add(book);
        await context.SaveChangesAsync();
        return (book.Id, book.Copies.Select(c => c.Id).ToArray());
    }

    private async Task<int> SeedUser(string email)
    {
        await using var context = _fixture.CreateContext();
        var user = new User
        {
            Name = email.Split('@')[0],
            Email = email,
            PasswordHash = "hash",
            Role = UserRole.User
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    // Cria direto no banco um empréstimo aberto e vencido (respeitando o CK_Loans_DueDate,
    // que exige DueDate >= data de BorrowedAt), e marca o exemplar como emprestado.
    private async Task SeedOverdueLoan(int copyId, int userId)
    {
        await using var context = _fixture.CreateContext();
        var borrowedAt = DateTime.UtcNow.AddDays(-20);
        context.Loans.Add(new Loan
        {
            UserId = userId,
            BookCopyId = copyId,
            BorrowedAt = borrowedAt,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
        });
        var copy = await context.BookCopies.SingleAsync(c => c.Id == copyId);
        copy.Status = BookCopyStatus.Loaned;
        await context.SaveChangesAsync();
    }

    // Cria um empréstimo aberto, mas deixa o exemplar Available de propósito, para que a próxima
    // tentativa passe pela checagem de status e esbarre no índice único parcial.
    private async Task SeedOpenLoanKeepingCopyAvailable(int copyId, int userId)
    {
        await using var context = _fixture.CreateContext();
        var borrowedAt = DateTime.UtcNow;
        context.Loans.Add(new Loan
        {
            UserId = userId,
            BookCopyId = copyId,
            BorrowedAt = borrowedAt,
            DueDate = DateOnly.FromDateTime(borrowedAt).AddDays(14)
        });
        await context.SaveChangesAsync();
    }
}
