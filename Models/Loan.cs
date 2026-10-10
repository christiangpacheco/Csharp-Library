namespace Library.Models;

public class Loan : ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BookCopyId { get; set; }

    public User User { get; set; } = null!;

    public BookCopy BookCopy { get; set; } = null!;

    public DateTime BorrowedAt { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
