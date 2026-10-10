using Library.DTOs.Account;
using Library.Models;

namespace Library.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult> RegisterAsync(RegisterUserDto dto);

    // Devolve o usuário quando e-mail e senha conferem e a conta está ativa; senão, null.
    Task<User?> ValidateCredentialsAsync(string email, string password);

    Task<User?> GetActiveUserAsync(int id);

    Task EnsureLibrarianAsync(string name, string email, string password);
}
