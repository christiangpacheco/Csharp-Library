using Library.DTOs.Account;

namespace Library.Services.Interfaces;

public interface IPasswordResetService
{
    // Cria um token de redefinição e devolve o valor em claro (para montar o link).
    // Devolve null quando não há usuário ativo com esse e-mail (para não revelar se a conta existe).
    Task<string?> CreateTokenAsync(string email);

    // Redefine a senha a partir do token do link. Falha se o token for inválido, expirado ou já usado.
    Task<ServiceResult> ResetAsync(ResetPasswordDto dto);
}
