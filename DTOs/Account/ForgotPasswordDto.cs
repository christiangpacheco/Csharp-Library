using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Account;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(254, ErrorMessage = "O e-mail pode ter no máximo 254 caracteres.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}
