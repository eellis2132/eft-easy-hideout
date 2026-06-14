using System.IO;
using EasyHideout.Helpers;
using EasyHideout.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<HideoutStation> HideoutStations { get; set; }
    public DbSet<HideoutLevel> HideoutLevels { get; set; }
    public DbSet<StationDependency> StationDependencies { get; set; }
    public DbSet<ItemRequirement> ItemRequirements { get; set; }
    public DbSet<ProfileStationLevel> ProfileStationLevels { get; set; }
    public DbSet<ItemCount> ItemCounts { get; set; }
    public DbSet<ImportantItem> ImportantItems { get; set; }
    public DbSet<IgnoredItem> IgnoredItems { get; set; }
    public DbSet<FocusNode> FocusNodes { get; set; }
    public DbSet<TraderRequirement> TraderRequirements { get; set; }
    public DbSet<TraderLoyaltyLevel> TraderLoyaltyLevels { get; set; }
    public DbSet<ItemPriceSnapshot> ItemPriceSnapshots { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbDir = Path.Combine(appData, AppMode.AppDataFolder);
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "EasyHideout.db");
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>().HasKey(x => x.Id);

        modelBuilder.Entity<ProfileStationLevel>()
            .HasIndex(x => new { x.ProfileId, x.StationId })
            .IsUnique();

        modelBuilder.Entity<ItemCount>()
            .HasIndex(x => new { x.ProfileId, x.TarkovItemId })
            .IsUnique();

        modelBuilder.Entity<ImportantItem>()
            .HasIndex(x => new { x.ProfileId, x.TarkovItemId })
            .IsUnique();

        modelBuilder.Entity<IgnoredItem>()
            .HasIndex(x => new { x.ProfileId, x.TarkovItemId })
            .IsUnique();

        modelBuilder.Entity<FocusNode>()
            .HasIndex(x => x.ProfileId)
            .IsUnique();

        modelBuilder.Entity<HideoutStation>()
            .HasIndex(x => x.TarkovStationId)
            .IsUnique();

        modelBuilder.Entity<StationDependency>()
            .HasOne(x => x.RequiredStation)
            .WithMany()
            .HasForeignKey(x => x.RequiredStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TraderLoyaltyLevel>()
            .HasIndex(x => new { x.TraderId, x.LoyaltyLevel })
            .IsUnique();

        modelBuilder.Entity<ItemPriceSnapshot>()
            .HasKey(x => x.TarkovItemId);
    }
}
