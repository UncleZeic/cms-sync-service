using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;

namespace CmsSyncService.Application.Services;

public class CmsEventApplicationService
{
    private readonly ICmsEntityRepository _cmsEntityRepository;

    public CmsEventApplicationService(ICmsEntityRepository cmsEntityRepository)
    {
        _cmsEntityRepository = cmsEntityRepository;
    }

    public async Task ProcessBatchAsync(IEnumerable<CmsEventDto> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var dto in events)
        {
            var cmsEvent = dto.ToDomain();

            var existingEntity = await _cmsEntityRepository.GetByIdAsync(
                cmsEvent.Id,
                cancellationToken);

            switch (cmsEvent.Type)
            {
                case CmsEventType.Delete:
                    if (existingEntity is not null)
                    {
                        _cmsEntityRepository.Remove(existingEntity);
                    }
                    break;

                case CmsEventType.Publish:
                    if (existingEntity is null)
                    {
                        var newEntity = CmsEntity.CreatePublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                    }
                    else
                    {
                        existingEntity.ApplyPublish(cmsEvent);
                    }
                    break;

                case CmsEventType.Unpublish:
                    if (existingEntity is null)
                    {
                        var newEntity = CmsEntity.CreateUnpublished(cmsEvent);
                        await _cmsEntityRepository.AddAsync(newEntity, cancellationToken);
                    }
                    else
                    {
                        existingEntity.ApplyUnpublish(cmsEvent);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported CMS event type '{cmsEvent.Type}'.");
            }
        }

        await _cmsEntityRepository.SaveChangesAsync(cancellationToken);
    }
}
