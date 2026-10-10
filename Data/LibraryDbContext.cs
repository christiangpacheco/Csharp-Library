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

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.DeletedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
