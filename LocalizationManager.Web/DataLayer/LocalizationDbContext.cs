using LocalizationManager.Web.DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalizationManager.Web.DataLayer;

public class LocalizationDbContext : DbContext
{
    public LocalizationDbContext(DbContextOptions<LocalizationDbContext> options)
        : base(options) { }

    public DbSet<CultureEntity> Cultures => Set<CultureEntity>();
    public DbSet<LocalizationRecordEntity> LocalizationRecords => Set<LocalizationRecordEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CultureEntity>().HasKey(c => c.Code);

        b.Entity<LocalizationRecordEntity>()
            .HasIndex(r => new { r.Env, r.CultureCode, r.Key })
            .IsUnique();

        b.Entity<LocalizationRecordEntity>()
            .HasOne(r => r.Culture)
            .WithMany()
            .HasForeignKey(r => r.CultureCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}