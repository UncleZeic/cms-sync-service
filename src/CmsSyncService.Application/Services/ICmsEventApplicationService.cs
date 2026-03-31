using CmsSyncService.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CmsSyncService.Application.Services;

public interface ICmsEventApplicationService
{
    Task ProcessBatchAsync(IEnumerable<CmsEventDto> events, CancellationToken cancellationToken = default);
}
