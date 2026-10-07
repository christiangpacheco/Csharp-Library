using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class LoanConfigurations : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans", t =>
        {
            t.HasCheckConstraint("CK_Loans_DueDate", "\"DueDate\" >= CAST(\"BorrowedAt\" AT TIME ZONE 'UTC' AS date)");
            t.HasCheckConstraint("CK_Loans_ReturnedAt", "\"ReturnedAt\" IS NULL OR \"ReturnedAt\" >= \"BorrowedAt\"");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.BorrowedAt).HasDefaultValueSql("now()");

        builder.Property(l => l.DueDate).HasColumnType("date");

        // Restrict preserva o histórico: usuário ou exemplar com empréstimos não é apagado.
        builder.HasOne(l => l.User)
            .WithMany(u => u.Loans)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.BookCopy)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.UserId);

        builder.HasIndex(l => l.BookCopyId);

        // Um exemplar só pode ter um empréstimo em aberto por vez.
        builder.HasIndex(l => l.BookCopyId, "IX_Loans_BookCopyId_Open")
            .IsUnique()
            .HasFilter("\"ReturnedAt\" IS NULL");
    }
}
