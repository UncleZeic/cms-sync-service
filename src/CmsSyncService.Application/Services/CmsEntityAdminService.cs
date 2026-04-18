using System.Threading;
using System.Threading.Tasks;
using CmsSyncService.Application.Repositories;
using CmsSyncService.Domain;

namespace CmsSyncService.Application.Services;

public class CmsEntityAdminService : ICmsEntityAdminService
{
    private readonly ICmsEntityRepository _repository;

    public CmsEntityAdminService(ICmsEntityRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> SetAdminDisabledAsync(string id, bool adminDisabled, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;
        if (entity.AdminDisabled == adminDisabled)
            return true;
        entity.SetAdminDisabled(adminDisabled);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
