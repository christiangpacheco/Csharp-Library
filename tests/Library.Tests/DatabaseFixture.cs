using Library.Data;
using Library.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Library.Tests;

// Sobe o esquema real (todas as migrations) em um banco PostgreSQL de teste, uma vez por execução.
// A string de conexão vem de LIBRARY_TEST_DB; sem ela, monta uma apontando para um banco
// dedicado "library_test" no mesmo PostgreSQL local do desenvolvimento (porta 5433, usuário admin,
// senha em POSTGRES_PASSWORD). Nunca toca no banco "library" do dia a dia.
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        ConnectionString = ResolveConnectionString();
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        // Recria do zero para garantir um esquema limpo, com índices e CHECKs iguais aos de produção.
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public LibraryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new LibraryDbContext(options);
    }

    public LoanService CreateLoanService(LibraryDbContext context, LoanRules? rules = null) =>
        new(context, Options.Create(rules ?? new LoanRules()));

    private static string ResolveConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
        return $"Host=localhost;Port=5433;Database=library_test;Username=admin;Password={password}";
    }
}
