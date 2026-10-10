using Library.Data;
using Library.DTOs.BookCopies;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class BookCopyService : IBookCopyService
{
    private const string UniqueViolation = "23505";

    private readonly LibraryDbContext _context;

    public BookCopyService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> CreateAsync(CreateBookCopyDto dto)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == dto.BookId);
        if (book is null)
        {
            return ServiceResult.Fail("Livro não encontrado.");
        }

        var code = dto.Code.Trim();
        if (code.Length == 0)
        {
            return ServiceResult.Fail("Informe o código do exemplar.", nameof(dto.Code));
        }

        if (await _context.BookCopies.AnyAsync(c => c.Code == code))
        {
            return DuplicateCode();
        }

        var copy = new BookCopy
        {
            BookId = dto.BookId,
            Code = code,
            Status = BookCopyStatus.Available
        };

        _context.BookCopies.Add(copy);

        try
        {
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(copy.Id);
        }
        // Dois cadastros com o mesmo código ao mesmo tempo passam pelo AnyAsync; o índice único barra o segundo.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            _context.Entry(copy).State = EntityState.Detached;
            return DuplicateCode();
        }
    }

    public async Task<ServiceResult> SetLostAsync(int id, bool lost)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy is null)
        {
            return ServiceResult.Fail("Exemplar não encontrado.");
        }

        // Exemplar emprestado não muda de status por aqui: a devolução (Leva 5) é que o libera.
        if (copy.Status == BookCopyStatus.Loaned)
        {
            return ServiceResult.Fail("Este exemplar está emprestado.");
        }

        copy.Status = lost ? BookCopyStatus.Lost : BookCopyStatus.Available;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok(copy.Id);
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy is null)
        {
            return ServiceResult.Fail("Exemplar não encontrado.");
        }

        if (!isActive && copy.Status == BookCopyStatus.Loaned)
        {
            return ServiceResult.Fail("Este exemplar está emprestado e não pode ser desativado.");
        }

        copy.IsActive = isActive;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok(copy.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy is null)
        {
            return ServiceResult.Fail("Exemplar não encontrado.");
        }

        // Excluir é o último passo: o exemplar precisa estar desativado.
        if (copy.IsActive)
        {
            return ServiceResult.Fail("Desative o exemplar antes de excluí-lo.");
        }

        // Exemplar com histórico de empréstimo não é apagado (preserva o histórico).
        if (await _context.Loans.IgnoreQueryFilters().AnyAsync(l => l.BookCopyId == id))
        {
            return ServiceResult.Fail("Este exemplar tem empréstimos registrados e não pode ser excluído.");
        }

        // Soft delete: o LibraryDbContext preenche DeletedAt em vez de apagar a linha.
        _context.BookCopies.Remove(copy);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok(copy.Id);
    }

    private static ServiceResult DuplicateCode() =>
        ServiceResult.Fail("Já existe um exemplar com este código.", nameof(CreateBookCopyDto.Code));
}
