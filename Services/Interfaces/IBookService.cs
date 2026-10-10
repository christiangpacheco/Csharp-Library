using Library.DTOs.Books;
using Library.Models;

namespace Library.Services.Interfaces;

public interface IBookService
{
    Task<BookIndexViewModel> SearchAsync(string? search, bool includeInactive, int page, int pageSize);

    Task<Book?> GetByIdAsync(int id);

    Task<ServiceResult> CreateAsync(CreateBookDto dto);

    Task<ServiceResult> UpdateAsync(UpdateBookDto dto);

    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    Task<ServiceResult> DeleteAsync(int id);
}
