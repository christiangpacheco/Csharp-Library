namespace Library.DTOs.Loans;

// Linha de leitura de um empréstimo, para as telas.
public class LoanResponseDto
{
    public int Id { get; init; }

    public int BookId { get; init; }

    public string BookTitle { get; init; } = string.Empty;

    public string BookCopyCode { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;

    public DateTime BorrowedAt { get; init; }

    public DateOnly DueDate { get; init; }

    public DateTime? ReturnedAt { get; init; }

    public bool IsReturned => ReturnedAt is not null;

    // Em aberto e com o prazo vencido.
    public bool IsOverdue => ReturnedAt is null && DueDate < DateOnly.FromDateTime(DateTime.UtcNow);
}
