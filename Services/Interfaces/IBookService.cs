using Library.DTOs.Books;
using Library.Models;

namespace Library.Services.Interfaces;

public interface IBookService
{
    Task<BookIndexViewModel> SearchAsync(string? search, int page, int pageSize);

    Task<Book?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(CreateBookDto dto);

    Task<ServiceResult> UpdateAsync(UpdateBookDto dto);

    Task<ServiceResult> DeleteAsync(int id);
}
