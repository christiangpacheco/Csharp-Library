using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
            t.HasCheckConstraint("CK_Users_Role", "\"Role\" IN ('User', 'Librarian')"));

        builder.HasKey(u => u.Id);

        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.Property(u => u.Name).HasMaxLength(150).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"DeletedAt\" IS NULL");

        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Novo registro nasce ativo. Com sentinel true, o EF só envia a coluna quando ela é false
        // e deixa o default do banco (true) valer no resto, inclusive nas linhas que já existiam.
        builder.Property(u => u.IsActive).HasDefaultValue(true).HasSentinel(true);

        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
    }
}
