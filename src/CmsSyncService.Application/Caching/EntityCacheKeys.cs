namespace CmsSyncService.Application.Caching;

public static class EntityCacheKeys
{
    public static string GetEntityKey(string id, bool isAdmin) => isAdmin ? $"entity_admin_{id}" : $"entity_viewer_{id}";
    public static string GetEntityListKey(bool isAdmin) => isAdmin ? "entities_admin" : "entities_viewer";
}
