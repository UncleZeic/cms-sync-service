using CmsSyncService.Application.Caching;
using Xunit;

public class EntityCacheKeysTests
{
    [Fact]
    public void GetEntityKey_Admin_And_Viewer_Different()
    {
        var adminKey = EntityCacheKeys.GetEntityKey("id1", true);
        var viewerKey = EntityCacheKeys.GetEntityKey("id1", false);
        Assert.NotEqual(adminKey, viewerKey);
    }

    [Fact]
    public void GetEntityListKey_Admin_And_Viewer_Different()
    {
        var adminKey = EntityCacheKeys.GetEntityListKey(true);
        var viewerKey = EntityCacheKeys.GetEntityListKey(false);
        Assert.NotEqual(adminKey, viewerKey);
    }

    [Fact]
    public void GetPagedEntityListKey_Uses_Params()
    {
        var k1 = EntityCacheKeys.GetPagedEntityListKey(true, 0, 100);
        var k2 = EntityCacheKeys.GetPagedEntityListKey(true, 1, 100);
        Assert.NotEqual(k1, k2);
        var k3 = EntityCacheKeys.GetPagedEntityListKey(false, 0, 100);
        Assert.NotEqual(k1, k3);
    }
}
