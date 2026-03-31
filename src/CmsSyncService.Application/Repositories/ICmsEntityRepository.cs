using CmsSyncService.Domain;

namespace CmsSyncService.Application.Repositories;

public interface ICmsEntityRepository
{
    Task<CmsEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task AddAsync(CmsEntity entity, CancellationToken cancellationToken = default);

    void Remove(CmsEntity entity);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}