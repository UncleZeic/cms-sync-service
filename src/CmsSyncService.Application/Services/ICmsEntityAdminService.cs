using System.Threading;
using System.Threading.Tasks;

namespace CmsSyncService.Application.Services;

public interface ICmsEntityAdminService
{
    Task<bool> SetAdminDisabledAsync(string id, bool adminDisabled, CancellationToken cancellationToken);
}
