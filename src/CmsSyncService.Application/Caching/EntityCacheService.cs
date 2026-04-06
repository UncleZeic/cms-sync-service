using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CmsSyncService.Application.Caching;

public class EntityCacheService : IEntityCacheService
{
    private readonly IMemoryCache _cache;
    private readonly int _entityCacheMinutes;

    public EntityCacheService(IMemoryCache cache, IOptions<CacheDurations> options)
    {
        _cache = cache;
        _entityCacheMinutes = options.Value.Entity;
    }

    public T? Get<T>(string key) where T : class
    {
        return _cache.TryGetValue(key, out var value) ? value as T : null;
    }

    public void Set<T>(string key, T value) where T : class
    {
        _cache.Set(key, value, TimeSpan.FromMinutes(_entityCacheMinutes));
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}

public interface IEntityCacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value) where T : class;
    void Remove(string key);
}

public class CacheDurations
{
    public int Entity { get; set; } = 5;
}
