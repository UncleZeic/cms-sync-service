using CmsSyncService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Infrastructure.Persistence;

public class CmsSyncDbContext : DbContext
{
    public CmsSyncDbContext(DbContextOptions<CmsSyncDbContext> options)
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
                .IsRequired();
            entity.Property(x => x.Version)
                .IsRequired();
            entity.Property(x => x.Published)
                .IsRequired();
            entity.Property(x => x.AdminDisabled)
                .IsRequired();
            entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();
        });
    }
}
