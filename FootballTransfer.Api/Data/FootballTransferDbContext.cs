using FootballTransfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballTransfer.Api.Data;

public class FootballTransferDbContext : DbContext
{
    public FootballTransferDbContext(DbContextOptions<FootballTransferDbContext> options)
        : base(options)
    {
    }

    public DbSet<TransferNews> TransferNews { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<Transfer> Transfers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transfer>()
            .Property(t => t.EstimatedFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TransferNews>()
            .Property(n => n.EstimatedFee)
            .HasPrecision(18, 2);
    }
}