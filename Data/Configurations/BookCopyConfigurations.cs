using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class BookCopyConfigurations : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies", t =>
            t.HasCheckConstraint("CK_BookCopies_Status", "\"Status\" IN ('Available', 'Loaned', 'Lost', 'Inactive')"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        // Livro com exemplares não pode ser apagado; desative os exemplares.
        builder.HasOne(c => c.Book)
            .WithMany(b => b.Copies)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
