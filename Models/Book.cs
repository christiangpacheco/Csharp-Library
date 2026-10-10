namespace Library.Models;

public class Book : ISoftDeletable
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string ISBN { get; set; } = string.Empty;

    public DateOnly PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAt { get; set; }
}
