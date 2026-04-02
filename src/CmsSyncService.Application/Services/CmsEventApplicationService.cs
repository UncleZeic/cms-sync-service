using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;
using Microsoft.Extensions.Logging;

namespace CmsSyncService.Application.Services;

public class CmsEventApplicationService : ICmsEventApplicationService
{
    private readonly ICmsEntityRepository _cmsEntityRepository;
    private readonly ILogger<CmsEventApplicationService> _logger;

    public CmsEventApplicationService(ICmsEntityRepository cmsEntityRepository, ILogger<CmsEventApplicationService> logger)
    {
        _cmsEntityRepository = cmsEntityRepository;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(IEnumerable<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        _logger.LogInformation("Processing batch of {Count} CMS events", events is ICollection<CmsEventDto> col ? col.Count : -1);

        foreach (var dto in events)
        {
            var cmsEvent = dto.ToDomain();
            _logger.LogDebug("Processing event: {EventType} for Id: {Id}", cmsEvent.Type, cmsEvent.Id);

            var existingEntity = await _cmsEntityRepository.GetByIdAsync(
                cmsEvent.Id,
                cancellationToken);

            switch (cmsEvent.Type)
            {
                case CmsEventType.Delete:
                    if (existingEntity is not null)
                    {
                        _logger.LogInformation("Deleting entity with Id: {Id}", cmsEvent.Id);
                        _cmsEntityRepository.Remove(existingEntity);
                    }
                    else
                    {
                        _logger.LogWarning("Delete event for non-existent entity Id: {Id}", cmsEvent.Id);
                    }
                    break;

                case CmsEventType.Publish:
                    if (existingEntity is null)
                    {
                        _logger.LogInformation("Publishing new entity with Id: {Id}", cmsEvent.Id);
                        var newEntity = CmsEntity.CreatePublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("Updating published entity with Id: {Id}", cmsEvent.Id);
                        existingEntity.ApplyPublish(cmsEvent);
                    }
                    break;

                case CmsEventType.Unpublish:
                    if (existingEntity is null)
                    {
                        _logger.LogInformation("Unpublishing new entity with Id: {Id}", cmsEvent.Id);
                        var newEntity = CmsEntity.CreateUnpublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("Updating unpublished entity with Id: {Id}", cmsEvent.Id);
                        existingEntity.ApplyUnpublish(cmsEvent);
                    }
                    break;

                default:
                    _logger.LogError("Unsupported CMS event type '{EventType}' for Id: {Id}", cmsEvent.Type, cmsEvent.Id);
                    throw new InvalidOperationException($"Unsupported CMS event type '{cmsEvent.Type}'.");
            }
        }

        _logger.LogInformation("Saving changes after processing batch");
        await _cmsEntityRepository.SaveChangesAsync(cancellationToken);
    }
}
