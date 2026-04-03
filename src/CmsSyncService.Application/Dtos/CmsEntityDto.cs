namespace CmsSyncService.Application.DTOs;

public class CmsEntityDto : ICmsEntityDto
{
    public string Id { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}