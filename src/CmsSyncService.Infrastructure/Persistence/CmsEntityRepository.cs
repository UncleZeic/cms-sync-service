
using CmsSyncService.Application.Repositories;
using CmsSyncService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Infrastructure.Persistence;

public class CmsEntityRepository : ICmsEntityRepository
{
    private readonly CmsSyncDbContext _writeDbContext;
    private readonly CmsSyncReadDbContext _readDbContext;

    public CmsEntityRepository(CmsSyncDbContext writeDbContext, CmsSyncReadDbContext readDbContext)
    {
        _writeDbContext = writeDbContext;
        _readDbContext = readDbContext;
    }

    public async Task<List<CmsEntity>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        return await _writeDbContext.CmsEntities
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<CmsEntity?> GetByIdVisibleToUserAsync(string id, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var query = _readDbContext.CmsEntities.AsQueryable();
        if (!isAdmin)
            query = query.Where(e => e.Published && !e.AdminDisabled);
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<CmsEntity>> GetVisibleToNormalUserAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _readDbContext.CmsEntities
            .Where(e => e.Published && !e.AdminDisabled)
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountVisibleToNormalUserAsync(CancellationToken cancellationToken = default)
    {
        return await _readDbContext.CmsEntities
            .CountAsync(e => e.Published && !e.AdminDisabled, cancellationToken);
    }

    public async Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _writeDbContext.CmsEntities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<CmsEntity>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _readDbContext.CmsEntities
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _readDbContext.CmsEntities.CountAsync(cancellationToken);
    }

    public async Task AddAsync(CmsEntity entity, CancellationToken cancellationToken = default)
    {
        await _writeDbContext.CmsEntities.AddAsync(entity, cancellationToken);
    }

    public void Remove(CmsEntity entity)
    {
        _writeDbContext.CmsEntities.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _writeDbContext.SaveChangesAsync(cancellationToken);
    }
}
