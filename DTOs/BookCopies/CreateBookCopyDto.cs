using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.BookCopies;

public class CreateBookCopyDto
{
    [Required]
    public int BookId { get; set; }

    // Código impresso na etiqueta do exemplar; é único no acervo (IX_BookCopies_Code).
    [Required(ErrorMessage = "Informe o código do exemplar.")]
    [StringLength(30, ErrorMessage = "O código pode ter no máximo 30 caracteres.")]
    [Display(Name = "Código")]
    public string Code { get; set; } = string.Empty;
}
