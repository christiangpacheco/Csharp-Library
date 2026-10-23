using Library.DTOs.Loans;
using Library.Models;

namespace Library.Services.Interfaces;

public interface ILoanService
{
    // Empresta um exemplar a um leitor, aplicando as regras (disponível, limite, atraso, prazo).
    Task<ServiceResult> BorrowAsync(int bookCopyId, int userId);

    // Registra a devolução; devolver duas vezes é erro.
    Task<ServiceResult> ReturnAsync(int loanId);

    // Empréstimos (abertos e devolvidos) de um leitor, mais recentes primeiro.
    Task<IReadOnlyList<LoanResponseDto>> GetUserLoansAsync(int userId);

    // Todos os empréstimos em aberto (para o bibliotecário gerenciar), vencidos primeiro.
    Task<IReadOnlyList<LoanResponseDto>> GetOpenLoansAsync();

    // Dados de um exemplar para a tela de empréstimo (ou null se não puder emprestar).
    Task<BookCopy?> GetAvailableCopyAsync(int bookCopyId);

    // Leitores ativos para escolher na tela de empréstimo.
    Task<IReadOnlyList<User>> GetBorrowersAsync();
}
