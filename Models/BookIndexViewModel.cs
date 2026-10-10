namespace Library.Models;

public class BookIndexViewModel
{
    public IReadOnlyList<BookListItem> Books { get; init; } = [];

    public string? Search { get; init; }

    public bool IncludeInactive { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int TotalCount { get; init; }
}

// Linha da lista de livros com a contagem de exemplares disponíveis ("X de Y").
public class BookListItem
{
    public Book Book { get; init; } = null!;

    public int AvailableCopies { get; init; }

    public int TotalCopies { get; init; }
}
