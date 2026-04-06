using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace CmsSyncService.Application.Services;

public class CmsEventApplicationService : ICmsEventApplicationService
{
    private readonly ICmsEntityRepository _cmsEntityRepository;
    private readonly ILogger<CmsEventApplicationService> _logger;
    private readonly IMemoryCache _cache;

    public CmsEventApplicationService(ICmsEntityRepository cmsEntityRepository, ILogger<CmsEventApplicationService> logger, IMemoryCache cache)
    {
        _cmsEntityRepository = cmsEntityRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task ProcessBatchAsync(IEnumerable<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        int total = events is ICollection<CmsEventDto> col ? col.Count : -1;
        int processed = 0, skipped = 0, validationFailed = 0, versionConflict = 0;
        _logger.LogInformation("Processing batch of {Count} CMS events", total);

        var affectedIds = new HashSet<string>();
        foreach (var dto in events)
        {
            // Validate DTO
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, validationResults, true))
            {
                validationFailed++;
                foreach (var result in validationResults)
                {
                    _logger.LogWarning("Event validation failed for Id: {Id}, Reason: {Reason}", dto.Id, result.ErrorMessage);
                }
                continue;
            }

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
                        processed++;
                        affectedIds.Add(cmsEvent.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Delete event for non-existent entity Id: {Id}", cmsEvent.Id);
                        skipped++;
                    }
                    break;

                case CmsEventType.Publish:
                    if (existingEntity is null)
                    {
                        _logger.LogInformation("Publishing new entity with Id: {Id}", cmsEvent.Id);
                        var newEntity = CmsEntity.CreatePublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                        processed++;
                        affectedIds.Add(cmsEvent.Id);
                    }
                    else if (cmsEvent.Version < existingEntity.Version)
                    {
                        _logger.LogWarning("Publish event version conflict for Id: {Id}. Incoming version: {IncomingVersion}, Current version: {CurrentVersion}", cmsEvent.Id, cmsEvent.Version, existingEntity.Version);
                        versionConflict++;
                        skipped++;
                    }
                    else
                    {
                        _logger.LogInformation("Updating published entity with Id: {Id}", cmsEvent.Id);
                        existingEntity.ApplyPublish(cmsEvent);
                        processed++;
                        affectedIds.Add(cmsEvent.Id);
                    }
                    break;


                case CmsEventType.Unpublish:
                    if (existingEntity is null)
                    {
                        _logger.LogInformation("Unpublishing new entity with Id: {Id}", cmsEvent.Id);
                        var newEntity = CmsEntity.CreateUnpublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                        processed++;
                        affectedIds.Add(cmsEvent.Id);
                    }
                    else if (cmsEvent.Version < existingEntity.Version)
                    {
                        _logger.LogWarning("Unpublish event version conflict for Id: {Id}. Incoming version: {IncomingVersion}, Current version: {CurrentVersion}", cmsEvent.Id, cmsEvent.Version, existingEntity.Version);
                        versionConflict++;
                        skipped++;
                    }
                    else
                    {
                        _logger.LogInformation("Updating unpublished entity with Id: {Id}", cmsEvent.Id);
                        existingEntity.ApplyUnpublish(cmsEvent);
                        processed++;
                        affectedIds.Add(cmsEvent.Id);
                    }
                    break;

                default:
                    _logger.LogError("Unsupported CMS event type '{EventType}' for Id: {Id}", cmsEvent.Type, cmsEvent.Id);
                    skipped++;
                    break;
            }
        }

        _logger.LogInformation("Batch summary: total={Total}, processed={Processed}, skipped={Skipped}, validationFailed={ValidationFailed}, versionConflicts={VersionConflicts}",
            total, processed, skipped, validationFailed, versionConflict);
        _logger.LogInformation("Saving changes after processing batch");
        await _cmsEntityRepository.SaveChangesAsync(cancellationToken);

        // Invalidate cache for affected entities and entity lists
        _cache.Remove("entities_admin");
        _cache.Remove("entities_viewer");
        foreach (var id in affectedIds)
        {
            _cache.Remove($"entity_admin_{id}");
            _cache.Remove($"entity_viewer_{id}");
        }
    }
}
