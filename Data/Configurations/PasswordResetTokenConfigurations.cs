using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class PasswordResetTokenConfigurations : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens", t =>
        {
            t.HasCheckConstraint("CK_PasswordResetTokens_Expiry", "\"ExpiresAt\" > \"CreatedAt\"");
            t.HasCheckConstraint("CK_PasswordResetTokens_UsedAt", "\"UsedAt\" IS NULL OR \"UsedAt\" >= \"CreatedAt\"");
        });

        builder.HasKey(t => t.Id);

        builder.HasQueryFilter(t => t.DeletedAt == null);

        // Hash em hexadecimal do SHA-256 (64 caracteres). Único para a busca ser direta e sem colisão.
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsFixedLength(false).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique().HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(t => t.UserId);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        // Restrict preserva o histórico, como no resto do projeto (nada é apagado de verdade).
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
