using CmsSyncService.Application;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CmsSyncService.Application.Caching;


public class EntityCacheService : IEntityCacheService
{
    private readonly IMemoryCache _cache;
    private readonly int _entityCacheMinutes;
    private readonly int _entityListCacheMinutes;

    public EntityCacheService(IMemoryCache cache, IOptions<CacheDurations> options)
    {
        _cache = cache;
        _entityCacheMinutes = options.Value.Entity;
        _entityListCacheMinutes = options.Value.EntityList;
    }
    public T? Get<T>(string key) where T : class
    {
        return _cache.TryGetValue(key, out var value) ? value as T : null;
    }


    // Simple in-memory locks for cache stampede protection
    private static readonly Dictionary<string, object> _locks = new();
    private static readonly object _locksGlobal = new();

    public T? GetOrCreate<T>(string key, Func<T> factory, bool isList = false) where T : class
    {
        if (_cache.TryGetValue(key, out var value) && value is T cached)
            return cached;

        // Stampede protection: lock per key
        object keyLock;
        lock (_locksGlobal)
        {
            if (!_locks.TryGetValue(key, out keyLock))
            {
                keyLock = new object();
                _locks[key] = keyLock;
            }
        }
        lock (keyLock)
        {
            if (_cache.TryGetValue(key, out value) && value is T cached2)
                return cached2;
            var created = factory();
            Set(key, created, isList);
            return created;
        }
    }

    public void Set<T>(string key, T value) where T : class => Set(key, value, false);

    private void Set<T>(string key, T value, bool isList) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (isList || key == EntityCacheKeys.GetEntityListKey(true) || key == EntityCacheKeys.GetEntityListKey(false))
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_entityListCacheMinutes);
            options.SlidingExpiration = TimeSpan.FromMinutes(_entityListCacheMinutes / 2.0);
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_entityCacheMinutes);
            options.SlidingExpiration = TimeSpan.FromMinutes(_entityCacheMinutes / 2.0);
        }
        _cache.Set(key, value, options);
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
    T? GetOrCreate<T>(string key, Func<T> factory, bool isList = false) where T : class;
}


