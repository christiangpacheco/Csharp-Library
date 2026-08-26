namespace Library.Models;

public class Loan
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BookId { get; set; }

    public User User { get; set; } = null!;

    public Book Book { get; set; } = null!;

    public DateTime BorrowedAt { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }
}