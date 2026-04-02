using CmsSyncService.Domain;

namespace CmsSyncService.Application.DTOs;

public static class CmsEntityMapping
{
    public static CmsEntityDto ToDto(this CmsEntity entity)
    {
        return new CmsEntityDto
        {
            Id = entity.Id,
            Payload = entity.Payload,
            Version = entity.Version,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    public static CmsEntityAdminDto ToAdminDto(this CmsEntity entity)
    {
        return new CmsEntityAdminDto
        {
            Id = entity.Id,
            Payload = entity.Payload,
            Version = entity.Version,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Published = entity.Published,
            AdminDisabled = entity.AdminDisabled
        };
    }
}