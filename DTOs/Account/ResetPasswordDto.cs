using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Account;

public class ResetPasswordDto
{
    // O token vem no link do e-mail e viaja num campo escondido do formulário.
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
