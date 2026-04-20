using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Services;

namespace CmsSyncService.Api.Messaging;

public sealed class DirectCmsEventPublisher : ICmsEventPublisher
{
    private readonly ICmsEventApplicationService _service;

    public DirectCmsEventPublisher(ICmsEventApplicationService service)
    {
        _service = service;
    }

    public bool PublishesAsynchronously => false;

    public Task PublishBatchAsync(IReadOnlyCollection<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        return _service.ProcessBatchAsync(events, cancellationToken);
    }
}
