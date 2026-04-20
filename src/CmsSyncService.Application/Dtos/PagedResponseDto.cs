namespace CmsSyncService.Application.DTOs;

public sealed class PagedResponseDto<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }

    public required int Total { get; init; }

    public required int Skip { get; init; }

    public required int Take { get; init; }
}
