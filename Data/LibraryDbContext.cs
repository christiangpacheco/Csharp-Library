using Library.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Soft delete: Remove() vira um UPDATE que preenche DeletedAt; o filtro global de cada configuration esconde o registro.
    // Users e Books também têm UpdatedAt, preenchido aqui para não depender de cada service.
    private void ApplyAuditRules()
    {
        var now = DateTime.UtcNow;

        // ToList(): mudar o State de uma entrada altera a coleção que o foreach percorre.
        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable)
            {
                // Grava só DeletedAt (e UpdatedAt abaixo), sem regravar as outras colunas lidas no FindAsync.
                entry.State = EntityState.Unchanged;
                entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = now;
                entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified = true;
            }

            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
