using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class BookConfigurations : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", t =>
            t.HasCheckConstraint("CK_Books_ISBN", "\"ISBN\" ~ '^[0-9]{13}$'"));

        builder.HasKey(b => b.Id);

        builder.HasQueryFilter(b => b.DeletedAt == null);

        builder.Property(b => b.Title).HasMaxLength(300).IsRequired();

        builder.Property(b => b.Author).HasMaxLength(200).IsRequired();

        builder.Property(b => b.ISBN).HasMaxLength(13).IsFixedLength(false).IsRequired();
        builder.HasIndex(b => b.ISBN).IsUnique().HasFilter("\"DeletedAt\" IS NULL");

        builder.Property(b => b.PublishedAt).HasColumnType("date");

        // Novo registro nasce ativo. Com sentinel true, o EF só envia a coluna quando ela é false
        // e deixa o default do banco (true) valer no resto, inclusive nas linhas que já existiam.
        builder.Property(b => b.IsActive).HasDefaultValue(true).HasSentinel(true);

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");
    }
}
