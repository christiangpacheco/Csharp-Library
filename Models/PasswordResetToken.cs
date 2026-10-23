namespace Library.Models;

// Token de redefinição de senha. Guardamos só o hash (SHA-256) do token; o valor em claro
// vai no link por e-mail e nunca é persistido. Uso único (UsedAt) e com expiração (ExpiresAt).
public class PasswordResetToken : ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
