using System;

namespace CmsSyncService.Domain;

public class CmsEntity
{
    public string Id { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public bool Published { get; private set; }

    public bool AdminDisabled { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    protected CmsEntity()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static CmsEntity CreatePublished(CmsEvent cmsEvent)
    {
        ArgumentNullException.ThrowIfNull(cmsEvent);

        return new CmsEntity
        {
            Id = cmsEvent.Id,
            Payload = cmsEvent.Payload ?? "{}",
            Version = cmsEvent.Version!.Value,
            Published = true,
            AdminDisabled = false,
            UpdatedAtUtc = cmsEvent.Timestamp
        };
    }

    public static CmsEntity CreateUnpublished(CmsEvent cmsEvent)
    {
        ArgumentNullException.ThrowIfNull(cmsEvent);

        return new CmsEntity
        {
            Id = cmsEvent.Id,
            Payload = cmsEvent.Payload ?? "{}",
            Version = cmsEvent.Version!.Value,
            Published = false,
            AdminDisabled = false,
            UpdatedAtUtc = cmsEvent.Timestamp
        };
    }

    public void ApplyPublish(CmsEvent cmsEvent)
    {
        ArgumentNullException.ThrowIfNull(cmsEvent);

        var version = cmsEvent.Version!.Value;
        if (version < Version) return; // reject genuinely stale events

        // Corner case: newer version means CMS modified without unpublishing first
        if (version > Version)
        {
            Payload = cmsEvent.Payload ?? "{}";
            Version = version;
        }

        Published = true; // always mark published if version >= stored
        UpdatedAtUtc = cmsEvent.Timestamp;
    }

    public void ApplyUnpublish(CmsEvent cmsEvent)
    {
        var version = cmsEvent.Version!.Value;
        if (version < Version) return; // reject genuinely stale events

        // Corner case: newer version means CMS modified without publishing first
        if (version > Version)
        {
            Payload = cmsEvent.Payload ?? "{}";
            Version = version;
        }

        Published = false; // always mark unpublished if version >= stored
        UpdatedAtUtc = cmsEvent.Timestamp;
    }

    public void SetAdminDisabled(bool value)
    {
        AdminDisabled = value;
    }

    public bool IsVisibleToNormalUser()
    {
        return Published && !AdminDisabled;
    }
}