using CmsSyncService.Domain;

namespace CmsSyncService.Application.Repositories;

public interface ICmsEntityRepository
{
    Task<List<CmsEntity>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);
    Task<CmsEntity?> GetByIdVisibleToUserAsync(string id, bool isAdmin, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<CmsEntity>> GetAllAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task<List<CmsEntity>> GetVisibleToNormalUserAsync(CancellationToken cancellationToken = default, bool asNoTracking = false);
    Task AddAsync(CmsEntity entity, CancellationToken cancellationToken = default);
    void Remove(CmsEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}