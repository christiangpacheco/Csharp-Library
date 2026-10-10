namespace Library.Models;

public class BookIndexViewModel
{
    public IReadOnlyList<Book> Books { get; init; } = [];

    public string? Search { get; init; }

    public bool IncludeInactive { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalCount { get; init; }
}
