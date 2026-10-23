using Library.Data;
using Library.DTOs.Loans;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Library.Services;

public class LoanService : ILoanService
{
    private const string UniqueViolation = "23505";

    private readonly LibraryDbContext _context;
    private readonly LoanRules _rules;

    public LoanService(LibraryDbContext context, IOptions<LoanRules> rules)
    {
        _context = context;
        _rules = rules.Value;
    }

    public async Task<ServiceResult> BorrowAsync(int bookCopyId, int userId)
    {
        var copy = await _context.BookCopies.FirstOrDefaultAsync(c => c.Id == bookCopyId);
        if (copy is null)
        {
            return ServiceResult.Fail("Exemplar não encontrado.");
        }

        if (!copy.IsActive || copy.Status != BookCopyStatus.Available)
        {
            return ServiceResult.Fail("Este exemplar não está disponível para empréstimo.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null || !user.IsActive)
        {
            return ServiceResult.Fail("Leitor não encontrado ou inativo.", nameof(CreateLoanDto.UserId));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var openLoans = await _context.Loans
            .Where(l => l.UserId == userId && l.ReturnedAt == null)
            .ToListAsync();

        if (openLoans.Any(l => l.DueDate < today))
        {
            return ServiceResult.Fail("Este leitor tem um empréstimo atrasado e não pode pegar outro.", nameof(CreateLoanDto.UserId));
        }

        if (openLoans.Count >= _rules.MaxOpenLoansPerUser)
        {
            return ServiceResult.Fail($"Este leitor já está no limite de {_rules.MaxOpenLoansPerUser} empréstimos em aberto.", nameof(CreateLoanDto.UserId));
        }

        var borrowedAt = DateTime.UtcNow;
        var loan = new Loan
        {
            UserId = userId,
            BookCopyId = bookCopyId,
            BorrowedAt = borrowedAt,
            DueDate = DateOnly.FromDateTime(borrowedAt).AddDays(_rules.LoanPeriodDays)
        };

        // Empréstimo e mudança de status do exemplar na mesma transação (um SaveChanges).
        copy.Status = BookCopyStatus.Loaned;
        _context.Loans.Add(loan);

        try
        {
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(loan.Id);
        }
        // Corrida: o índice único parcial IX_Loans_BookCopyId_Open barra um segundo empréstimo aberto do mesmo exemplar.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            _context.Entry(loan).State = EntityState.Detached;
            return ServiceResult.Fail("Este exemplar já está emprestado.");
        }
    }

    public async Task<ServiceResult> ReturnAsync(int loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.BookCopy)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan is null)
        {
            return ServiceResult.Fail("Empréstimo não encontrado.");
        }

        if (loan.ReturnedAt is not null)
        {
            return ServiceResult.Fail("Este empréstimo já foi devolvido.");
        }

        loan.ReturnedAt = DateTime.UtcNow;

        // Exemplar perdido continua perdido; só volta a Available quem estava emprestado.
        if (loan.BookCopy.Status == BookCopyStatus.Loaned)
        {
            loan.BookCopy.Status = BookCopyStatus.Available;
        }

        await _context.SaveChangesAsync();
        return ServiceResult.Ok(loan.Id);
    }

    public async Task<IReadOnlyList<LoanResponseDto>> GetUserLoansAsync(int userId)
    {
        var loans = await OpenableQuery()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ReturnedAt == null)
            .ThenByDescending(l => l.BorrowedAt)
            .ToListAsync();

        return loans.Select(Project).ToList();
    }

    public async Task<IReadOnlyList<LoanResponseDto>> GetOpenLoansAsync()
    {
        var loans = await OpenableQuery()
            .Where(l => l.ReturnedAt == null)
            .OrderBy(l => l.DueDate)
            .ThenBy(l => l.Id)
            .ToListAsync();

        return loans.Select(Project).ToList();
    }

    private IQueryable<Loan> OpenableQuery() =>
        _context.Loans
            .AsNoTracking()
            .Include(l => l.BookCopy).ThenInclude(c => c.Book)
            .Include(l => l.User);

    public Task<BookCopy?> GetAvailableCopyAsync(int bookCopyId)
    {
        return _context.BookCopies
            .AsNoTracking()
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.Id == bookCopyId && c.IsActive && c.Status == BookCopyStatus.Available);
    }

    public async Task<IReadOnlyList<User>> GetBorrowersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    // BookCopy/Book vêm por navegação; os filtros globais (DeletedAt == null) já se aplicam.
    private static LoanResponseDto Project(Loan l) => new()
    {
        Id = l.Id,
        BookId = l.BookCopy.BookId,
        BookTitle = l.BookCopy.Book.Title,
        BookCopyCode = l.BookCopy.Code,
        UserName = l.User.Name,
        UserEmail = l.User.Email,
        BorrowedAt = l.BorrowedAt,
        DueDate = l.DueDate,
        ReturnedAt = l.ReturnedAt
    };
}
