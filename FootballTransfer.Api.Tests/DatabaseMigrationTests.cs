using FootballTransfer.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Tests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public void Production_hardening_migration_is_discoverable()
    {
        var options = new DbContextOptionsBuilder<FootballTransferDbContext>()
            .UseNpgsql("Host=localhost;Database=discovery;Username=postgres;Password=postgres")
            .Options;
        using var context = new FootballTransferDbContext(options);

        Assert.Contains("20261012030000_HardenPublicSchemaAndCorrectFees", context.Database.GetMigrations());
        Assert.Contains("20261012030001_AddEventAggregationAndSourceStatus", context.Database.GetMigrations());
        Assert.Contains("20261020222213_AddLoanFlag", context.Database.GetMigrations());
        Assert.Contains("20261020233312_AddEvidenceRebuildMaintenanceJob", context.Database.GetMigrations());
    }

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
