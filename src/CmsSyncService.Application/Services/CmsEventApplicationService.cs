using System.ComponentModel.DataAnnotations;
using CmsSyncService.Application.Caching;
using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;

using Microsoft.Extensions.Logging;

namespace CmsSyncService.Application.Services;

public class CmsEventApplicationService : ICmsEventApplicationService
{
    private readonly ICmsEntityRepository _cmsEntityRepository;
    private readonly ILogger<CmsEventApplicationService> _logger;
    private readonly IEntityCacheService _cache;

    public CmsEventApplicationService(ICmsEntityRepository cmsEntityRepository, ILogger<CmsEventApplicationService> logger, IEntityCacheService cache)
    {
        _cmsEntityRepository = cmsEntityRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task ProcessBatchAsync(IEnumerable<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        var batch = events as IReadOnlyCollection<CmsEventDto> ?? events.ToList();
        int total = batch.Count;
        int processed = 0, skipped = 0, validationFailed = 0, versionConflict = 0;
        _logger.LogInformation("Processing batch of {Count} CMS events", total);

        var affectedIds = new HashSet<string>();
        await _cmsEntityRepository.ExecuteInTransactionAsync(async () =>
        {
            // Fetch all necessary entities inside the transaction so updates are committed atomically with SaveChanges.
            var eventIds = batch
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .Select(e => e.Id.Trim())
                .Distinct()
                .ToList();
            var entities = await _cmsEntityRepository.GetByIdsAsync(eventIds, cancellationToken) ?? new List<CmsEntity>();
            var existingEntities = entities.ToDictionary(e => e.Id);

            foreach (var dto in batch)
            {
                // Validate DTO
                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(dto);
                if (!Validator.TryValidateObject(dto, context, validationResults, true))
                {
                    validationFailed++;
                    foreach (var result in validationResults)
                    {
                        _logger.LogWarning("Event validation failed for Id: {Id}, Reason: {Reason}", dto.Id, result.ErrorMessage);
                    }
                    continue;
                }

                var cmsEvent = dto.ToNormalized();
                _logger.LogDebug("Processing event: {EventType} for Id: {Id}", cmsEvent.Type, cmsEvent.Id);

                existingEntities.TryGetValue(cmsEvent.Id, out var existingEntity);

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
        }, cancellationToken);

        // Invalidate cache after the transaction commits.
        _cache.Remove(EntityCacheKeys.GetDefaultPagedEntityListKey(true));
        _cache.Remove(EntityCacheKeys.GetDefaultPagedEntityListKey(false));
        foreach (var id in affectedIds)
        {
            _cache.Remove(EntityCacheKeys.GetEntityKey(id, true));
            _cache.Remove(EntityCacheKeys.GetEntityKey(id, false));
        }
    }
}
