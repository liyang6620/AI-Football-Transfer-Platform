using FootballTransfer.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Tests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task All_migrations_apply_to_postgresql()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<FootballTransferDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new FootballTransferDbContext(options);

        await context.Database.MigrateAsync();

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
