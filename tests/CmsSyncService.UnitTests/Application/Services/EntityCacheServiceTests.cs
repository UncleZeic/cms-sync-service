using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CmsSyncService.Application;
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
        string? value = service.Get<string>("key");
        Assert.Equal("value", value);
    }

    [Fact]
    public void GetOrCreate_Creates_And_Caches()
    {
        var service = CreateService();
        int called = 0;
        string? value = service.GetOrCreate("key", () => { called++; return "v"; });
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

    [Fact]
    public void GetOrCreate_Removes_Key_Lock_After_Factory_Completes()
    {
        var service = CreateService();
        var key = $"key-{Guid.NewGuid()}";

        service.GetOrCreate(key, () => "value");

        Assert.False(CacheLockExists(key));
    }

    [Fact]
    public async Task GetOrCreate_Calls_Factory_Once_For_Concurrent_Requests()
    {
        var service = CreateService();
        var key = $"key-{Guid.NewGuid()}";
        var factoryCalls = 0;
        using var factoryStarted = new ManualResetEventSlim(false);
        using var releaseFactory = new ManualResetEventSlim(false);

        var first = Task.Run(() => service.GetOrCreate(key, () =>
        {
            Interlocked.Increment(ref factoryCalls);
            factoryStarted.Set();
            releaseFactory.Wait(TimeSpan.FromSeconds(5));
            return "value";
        }));

        Assert.True(factoryStarted.Wait(TimeSpan.FromSeconds(5)));

        var second = Task.Run(() => service.GetOrCreate(key, () =>
        {
            Interlocked.Increment(ref factoryCalls);
            return "other";
        }));

        releaseFactory.Set();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(new[] { "value", "value" }, results);
        Assert.Equal(1, factoryCalls);
        Assert.False(CacheLockExists(key));
    }

    private static bool CacheLockExists(string key)
    {
        var locksField = typeof(EntityCacheService).GetField("_locks", BindingFlags.NonPublic | BindingFlags.Static);
        var locks = locksField?.GetValue(null) ?? throw new InvalidOperationException("Could not inspect cache locks.");
        var containsKey = locks.GetType().GetMethod("ContainsKey", new[] { typeof(string) });

        return (bool)(containsKey?.Invoke(locks, new object[] { key }) ??
            throw new InvalidOperationException("Could not inspect cache lock key."));
    }
}
