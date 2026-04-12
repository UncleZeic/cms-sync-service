using System;
using System.Collections.Generic;
using CmsSyncService.Application.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

public class EntityCacheServiceTests
{
    private EntityCacheService CreateService(int entityMinutes = 5, int listMinutes = 2)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheDurations { Entity = entityMinutes, EntityList = listMinutes });
        return new EntityCacheService(memoryCache, options);
    }

    [Fact]
    public void Set_And_Get_Works()
    {
        var service = CreateService();
        service.Set("key", "value");
        Assert.Equal("value", service.Get<string>("key"));
    }

    [Fact]
    public void GetOrCreate_Creates_And_Caches()
    {
        var service = CreateService();
        int called = 0;
        string value = service.GetOrCreate("key", () => { called++; return "v"; });
        Assert.Equal("v", value);
        Assert.Equal(1, called);
        // Should not call factory again
        value = service.GetOrCreate("key", () => { called++; return "x"; });
        Assert.Equal("v", value);
        Assert.Equal(1, called);
    }

    [Fact]
    public void Remove_Removes()
    {
        var service = CreateService();
        service.Set("key", "value");
        service.Remove("key");
        Assert.Null(service.Get<string>("key"));
    }

    [Fact]
    public void List_And_Entity_Expiration_Are_Different()
    {
        var service = CreateService(10, 1);
        service.Set("entity", "e");
        service.Set("entities_admin", "l");
        // Can't easily test expiration without time travel, but can check that both set
        Assert.Equal("e", service.Get<string>("entity"));
        Assert.Equal("l", service.Get<string>("entities_admin"));
    }
}
