using System;

namespace CmsSyncService.Domain;

public sealed class CmsEvent
{
    public string Id { get; init; } = string.Empty;
    public CmsEventType Type { get; init; }
    public string? Payload { get; init; }
    public int? Version { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}