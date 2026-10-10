using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class BookCopyConfigurations : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies", t =>
            t.HasCheckConstraint("CK_BookCopies_Status", "\"Status\" IN ('Available', 'Loaned', 'Lost')"));

        builder.HasKey(c => c.Id);

        builder.HasQueryFilter(c => c.DeletedAt == null);

        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Novo registro nasce ativo. Com sentinel true, o EF só envia a coluna quando ela é false
        // e deixa o default do banco (true) valer no resto, inclusive nas linhas que já existiam.
        builder.Property(c => c.IsActive).HasDefaultValue(true).HasSentinel(true);

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        // Nada é apagado de verdade (soft delete); a FK Restrict é a última proteção do histórico.
        builder.HasOne(c => c.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
