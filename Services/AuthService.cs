using Library.Data;
using Library.DTOs.Account;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class AuthService : IAuthService
{
    private const string UniqueViolation = "23505";
    private const string DuplicateEmail = "Já existe uma conta com este e-mail.";

    private readonly LibraryDbContext _context;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(LibraryDbContext context, IPasswordHasher<User> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task<ServiceResult> RegisterAsync(RegisterUserDto dto)
    {
        var email = NormalizeEmail(dto.Email);

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return ServiceResult.Fail(DuplicateEmail, nameof(RegisterUserDto.Email));
        }

        // Cadastro público cria sempre um usuário comum; bibliotecário só pelo seed.
        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            Role = UserRole.User
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            return ServiceResult.Fail(DuplicateEmail, nameof(RegisterUserDto.Email));
        }

        return ServiceResult.Ok(user.Id);
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        var normalized = NormalizeEmail(email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    public Task<User?> GetActiveUserAsync(int id)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task EnsureLibrarianAsync(string name, string email, string password)
    {
        var normalized = NormalizeEmail(email);

        // Idempotente: se a conta já existe (mesmo que como usuário comum), não mexe nela.
        if (await _context.Users.AnyAsync(u => u.Email == normalized))
        {
            return;
        }

        var user = new User
        {
            Name = name.Trim(),
            Email = normalized,
            Role = UserRole.Librarian
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
