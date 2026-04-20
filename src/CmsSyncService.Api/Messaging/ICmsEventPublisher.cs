using CmsSyncService.Application.DTOs;

namespace CmsSyncService.Api.Messaging;

public interface ICmsEventPublisher
{
    bool PublishesAsynchronously { get; }

    Task PublishBatchAsync(IReadOnlyCollection<CmsEventDto> events, CancellationToken cancellationToken = default);
}
