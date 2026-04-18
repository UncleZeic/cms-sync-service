namespace CmsSyncService.Application.Caching;

public static class EntityCacheKeys
{
    public const int DefaultSkip = 0;
    public const int DefaultTake = 100;

    public static string GetEntityKey(string id, bool isAdmin) => isAdmin ? $"entity_admin_{id}" : $"entity_viewer_{id}";
    public static string GetEntityListKey(bool isAdmin) => isAdmin ? "entities_admin" : "entities_viewer";
    public static string GetPagedEntityListKey(bool isAdmin, int skip, int take) =>
        isAdmin ? $"entities_admin_{skip}_{take}" : $"entities_viewer_{skip}_{take}";

    public static string GetDefaultPagedEntityListKey(bool isAdmin) =>
        GetPagedEntityListKey(isAdmin, DefaultSkip, DefaultTake);
}
