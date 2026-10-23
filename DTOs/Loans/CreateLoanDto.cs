using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Loans;

public class CreateLoanDto
{
    [Required]
    public int BookCopyId { get; set; }

    [Required(ErrorMessage = "Escolha o leitor.")]
    [Display(Name = "Leitor")]
    public int UserId { get; set; }
}
