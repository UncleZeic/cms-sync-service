using CmsSyncService.Application.Repositories;
using CmsSyncService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Infrastructure.Persistence;

public class CmsEntityRepository : ICmsEntityRepository
{
    private readonly CmsSyncDbContext _dbContext;

    public CmsEntityRepository(CmsSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<CmsEntity>> GetAllAsync(CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CmsEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.CmsEntities.AddAsync(entity, cancellationToken);
    }

    public void Remove(CmsEntity entity)
    {
        _dbContext.CmsEntities.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}