using Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Messaging.Tests.Helpers;

public sealed class TestDbContext : DbContext
{
    public static readonly InMemoryDatabaseRoot Root = new();

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public static TestDbContext Create(string databaseName) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(databaseName, Root).Options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOutboxConfiguration();
    }
}
