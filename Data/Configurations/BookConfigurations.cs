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

        builder.Property(b => b.Title).HasMaxLength(300).IsRequired();

        builder.Property(b => b.Author).HasMaxLength(200).IsRequired();

        builder.Property(b => b.ISBN).HasMaxLength(13).IsFixedLength(false).IsRequired();
        builder.HasIndex(b => b.ISBN).IsUnique();

        builder.Property(b => b.PublishedAt).HasColumnType("date");

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");
    }
}
