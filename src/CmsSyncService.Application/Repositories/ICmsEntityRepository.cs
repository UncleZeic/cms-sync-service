using CmsSyncService.Domain;

namespace CmsSyncService.Application.Repositories;

public interface ICmsEntityRepository
{
    Task<List<CmsEntity>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<CmsEntity?> GetByIdVisibleToUserAsync(string id, bool isAdmin, CancellationToken cancellationToken = default);
    Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<CmsEntity>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<List<CmsEntity>> GetVisibleToNormalUserAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task AddAsync(CmsEntity entity, CancellationToken cancellationToken = default);
    void Remove(CmsEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
