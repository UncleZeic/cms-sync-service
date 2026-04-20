using CmsSyncService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CmsSyncService.Infrastructure.Persistence;

public class CmsSyncDbContext : DbContext
{
    public CmsSyncDbContext(DbContextOptions<CmsSyncDbContext> options)
        : base(options)
    {
    }

    protected CmsSyncDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<CmsEntity> CmsEntities => Set<CmsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CmsEntity>(entity =>
        {
            entity.ToTable("cms_entities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(x => x.Payload)
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(x => x.Version)
                .IsRequired();
            entity.Property(x => x.Published)
                .IsRequired();
            entity.Property(x => x.AdminDisabled)
                .IsRequired();
            var updatedAtProperty = entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                updatedAtProperty.HasConversion(new DateTimeOffsetToBinaryConverter());
            }
            entity.HasIndex(x => new { x.UpdatedAtUtc, x.Id })
                .IsDescending(true, false);
            entity.HasIndex(x => new { x.Published, x.AdminDisabled, x.UpdatedAtUtc, x.Id })
                .IsDescending(false, false, true, false);
        });
    }
}
