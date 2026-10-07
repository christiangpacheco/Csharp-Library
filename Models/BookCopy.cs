namespace Library.Models;

public class BookCopy
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;

    public DateTime CreatedAt { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
