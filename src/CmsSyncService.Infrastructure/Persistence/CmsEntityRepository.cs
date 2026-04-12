
using CmsSyncService.Application.Repositories;
using CmsSyncService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Infrastructure.Persistence;

public class CmsEntityRepository : ICmsEntityRepository
{
    public async Task<List<CmsEntity>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CmsEntities
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }
    public async Task<CmsEntity?> GetByIdVisibleToUserAsync(string id, bool isAdmin, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        if (!isAdmin)
            query = query.Where(e => e.Published && !e.AdminDisabled);
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    private readonly CmsSyncDbContext _dbContext;

    public CmsEntityRepository(CmsSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<List<CmsEntity>> GetVisibleToNormalUserAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.Where(e => e.Published && !e.AdminDisabled)
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<CmsEntity>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default, bool asNoTracking = false)
    {
        var query = _dbContext.CmsEntities.AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
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