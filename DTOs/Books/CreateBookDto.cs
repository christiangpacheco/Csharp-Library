using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Books;

public class CreateBookDto
{
    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(300, ErrorMessage = "O título pode ter no máximo 300 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o autor.")]
    [StringLength(200, ErrorMessage = "O autor pode ter no máximo 200 caracteres.")]
    [Display(Name = "Autor")]
    public string Author { get; set; } = string.Empty;

    // Aceita hífens e espaços (978-0-13-235088-4); o BookService guarda só os 13 dígitos.
    [Required(ErrorMessage = "Informe o ISBN.")]
    [RegularExpression(@"^[0-9\- ]{13,17}$", ErrorMessage = "O ISBN deve ter 13 dígitos.")]
    [Display(Name = "ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de publicação.")]
    [Display(Name = "Publicação")]
    public DateOnly? PublishedAt { get; set; }
}
