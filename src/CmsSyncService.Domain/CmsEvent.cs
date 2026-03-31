namespace CmsSyncService.Domain;

public sealed class CmsEvent
{
    public string ExternalId { get; init; } = string.Empty;
    public CmsEventType Type { get; init; }
    public string? Payload { get; init; }
    public int? Version { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}