using System.Security.Cryptography;
using Library.Data;
using Library.DTOs.Account;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Library.Services;

public class PasswordResetService : IPasswordResetService
{
    // Mensagem única para o reset: não revela se o token não existe, expirou ou já foi usado.
    private const string InvalidToken = "Link inválido ou expirado. Peça um novo.";

    private readonly LibraryDbContext _context;
    private readonly IPasswordHasher<User> _hasher;
    private readonly PasswordResetOptions _options;

    public PasswordResetService(LibraryDbContext context, IPasswordHasher<User> hasher, IOptions<PasswordResetOptions> options)
    {
        _context = context;
        _hasher = hasher;
        _options = options.Value;
    }

    public async Task<string?> CreateTokenAsync(string email)
    {
        var normalized = NormalizeEmail(email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive);
        if (user is null)
        {
            return null;
        }

        // Um pedido novo invalida os anteriores ainda não usados: só o último link vale.
        await InvalidateOpenTokensAsync(user.Id);

        var token = GenerateToken();
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes)
        });

        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<ServiceResult> ResetAsync(ResetPasswordDto dto)
    {
        var hash = Hash(dto.Token);
        var token = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        // Token inexistente, já usado, expirado ou de usuário inativo: tudo vira a mesma resposta.
        if (token is null || token.UsedAt is not null || token.ExpiresAt <= DateTime.UtcNow || !token.User.IsActive)
        {
            return ServiceResult.Fail(InvalidToken);
        }

        token.User.PasswordHash = _hasher.HashPassword(token.User, dto.Password);
        token.UsedAt = DateTime.UtcNow;

        // Qualquer outro token aberto do usuário também deixa de valer após a troca.
        await InvalidateOpenTokensAsync(token.UserId, exceptId: token.Id);

        await _context.SaveChangesAsync();
        return ServiceResult.Ok(token.UserId);
    }

    private async Task InvalidateOpenTokensAsync(int userId, int? exceptId = null)
    {
        var open = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && (exceptId == null || t.Id != exceptId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var t in open)
        {
            t.UsedAt = now;
        }
    }

    // 32 bytes aleatórios em base64url: entropia alta o bastante para guardar só o hash (sem hashing lento).
    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
