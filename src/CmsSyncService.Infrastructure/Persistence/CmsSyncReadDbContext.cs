using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Infrastructure.Persistence;

public sealed class CmsSyncReadDbContext : CmsSyncDbContext
{
    public CmsSyncReadDbContext(DbContextOptions<CmsSyncReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}
