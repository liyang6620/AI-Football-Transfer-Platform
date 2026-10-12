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
    public DbSet<SourceSyncState> SourceSyncStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transfer>()
            .Property(t => t.EstimatedFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TransferNews>()
            .Property(n => n.EstimatedFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TransferNews>()
            .HasIndex(n => n.Url)
            .IsUnique();

        modelBuilder.Entity<Transfer>()
            .HasIndex(t => t.TransferNewsId)
            .IsUnique();

        modelBuilder.Entity<Transfer>()
            .HasIndex(t => t.EventKey)
            .IsUnique();

        modelBuilder.Entity<Transfer>()
            .HasOne(t => t.TransferNews)
            .WithMany()
            .HasForeignKey(t => t.TransferNewsId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TransferNews>()
            .HasOne(n => n.Transfer)
            .WithMany(t => t.Sources)
            .HasForeignKey(n => n.TransferId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SourceSyncState>()
            .HasIndex(s => s.Name)
            .IsUnique();
    }
}
