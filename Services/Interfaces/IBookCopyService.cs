using Library.DTOs.BookCopies;

namespace Library.Services.Interfaces;

public interface IBookCopyService
{
    Task<ServiceResult> CreateAsync(CreateBookCopyDto dto);

    // Marca o exemplar como perdido ou o traz de volta para disponível (reverter engano).
    Task<ServiceResult> SetLostAsync(int id, bool lost);

    // Ativa ou desativa o exemplar (desativado não aparece para empréstimo na Leva 5).
    Task<ServiceResult> SetActiveAsync(int id, bool isActive);

    // Soft delete: só exemplar inativo e sem empréstimos.
    Task<ServiceResult> DeleteAsync(int id);
}
