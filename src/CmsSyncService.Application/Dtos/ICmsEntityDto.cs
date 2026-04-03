namespace CmsSyncService.Application.DTOs;

public interface ICmsEntityDto
{
    string Id { get; }
    string Payload { get; }
    int Version { get; }
    DateTimeOffset UpdatedAtUtc { get; }
}