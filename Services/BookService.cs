using Library.Data;
using Library.DTOs.Books;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class BookService : IBookService
{
    private const string UniqueViolation = "23505";

    private readonly LibraryDbContext _context;

    public BookService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<BookIndexViewModel> SearchAsync(string? search, bool includeInactive, int page, int pageSize)
    {
        var query = _context.Books.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        search = search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            // Escapa curingas do LIKE para que % e _ sejam buscados como texto literal.
            var escaped = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            var isbn = NormalizeIsbn(search);

            query = query.Where(b =>
                EF.Functions.ILike(b.Title, pattern)
                || EF.Functions.ILike(b.Author, pattern)
                || (isbn.Length > 0 && b.ISBN.Contains(isbn)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        // As contagens de exemplares respeitam o filtro global (DeletedAt == null): só contam os não excluídos.
        var books = await query
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookListItem
            {
                Book = b,
                TotalCopies = b.Copies.Count,
                AvailableCopies = b.Copies.Count(c => c.IsActive && c.Status == BookCopyStatus.Available)
            })
            .ToListAsync();

        return new BookIndexViewModel
        {
            Books = books,
            Search = search,
            IncludeInactive = includeInactive,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }

    public Task<Book?> GetByIdAsync(int id)
    {
        return _context.Books
            .AsNoTracking()
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<ServiceResult> CreateAsync(CreateBookDto dto)
    {
        var isbn = NormalizeIsbn(dto.ISBN);
        if (isbn.Length != 13)
        {
            return ServiceResult.Fail("O ISBN deve ter 13 dígitos.", nameof(dto.ISBN));
        }

        if (await _context.Books.AnyAsync(b => b.ISBN == isbn))
        {
            return DuplicateIsbn();
        }

        var book = new Book
        {
            Title = dto.Title.Trim(),
            Author = dto.Author.Trim(),
            ISBN = isbn,
            PublishedAt = dto.PublishedAt!.Value
        };

        _context.Books.Add(book);

        return await SaveAsync(book);
    }

    public async Task<ServiceResult> UpdateAsync(UpdateBookDto dto)
    {
        var book = await _context.Books.FindAsync(dto.Id);
        if (book is null)
        {
            return ServiceResult.Fail("Livro não encontrado.");
        }

        var isbn = NormalizeIsbn(dto.ISBN);
        if (isbn.Length != 13)
        {
            return ServiceResult.Fail("O ISBN deve ter 13 dígitos.", nameof(dto.ISBN));
        }

        if (await _context.Books.AnyAsync(b => b.ISBN == isbn && b.Id != dto.Id))
        {
            return DuplicateIsbn();
        }

        book.Title = dto.Title.Trim();
        book.Author = dto.Author.Trim();
        book.ISBN = isbn;
        book.PublishedAt = dto.PublishedAt!.Value;

        return await SaveAsync(book);
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return ServiceResult.Fail("Livro não encontrado.");
        }

        book.IsActive = isActive;
        await _context.SaveChangesAsync();

        return ServiceResult.Ok(id);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            return ServiceResult.Fail("Livro não encontrado.");
        }

        // Excluir é o último passo: o livro precisa estar desativado e sem exemplares.
        if (book.IsActive)
        {
            return ServiceResult.Fail("Desative o livro antes de excluí-lo.");
        }

        if (await _context.BookCopies.AnyAsync(c => c.BookId == id))
        {
            return ServiceResult.Fail("Este livro tem exemplares cadastrados e não pode ser excluído. Exclua os exemplares antes.");
        }

        // Soft delete: o LibraryDbContext preenche DeletedAt em vez de apagar a linha.
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok(id);
    }

    private async Task<ServiceResult> SaveAsync(Book book)
    {
        try
        {
            await _context.SaveChangesAsync();
            return ServiceResult.Ok(book.Id);
        }
        // Duas requisições com o mesmo ISBN ao mesmo tempo passam pelo AnyAsync; o índice único IX_Books_ISBN barra a segunda.
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            _context.Entry(book).State = EntityState.Detached;
            return DuplicateIsbn();
        }
    }

    private static ServiceResult DuplicateIsbn() =>
        ServiceResult.Fail("Já existe um livro com este ISBN.", nameof(CreateBookDto.ISBN));

    private static string NormalizeIsbn(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
