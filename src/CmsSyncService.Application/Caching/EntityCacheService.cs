
using CmsSyncService.Application;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
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

    // Improved in-memory locks for cache stampede protection
    // Use WeakReference to avoid unbounded memory growth
    private static readonly ConcurrentDictionary<string, WeakReference<object>> _locks = new();
    private static int _cleanupCounter = 0;

    public T? GetOrCreate<T>(string key, Func<T> factory, bool isList = false) where T : class
    {
        if (_cache.TryGetValue(key, out var value) && value is T cached)
            return cached;

        // Stampede protection: lock per key using WeakReference
        object keyLock = _locks.GetOrAdd(key, _ => new WeakReference<object>(new object()));
        if (keyLock is WeakReference<object> weakRef)
        {
            if (!weakRef.TryGetTarget(out var realLock))
            {
                realLock = new object();
                _locks[key] = new WeakReference<object>(realLock);
            }
            // Periodically clean up dead WeakReferences
            if (Interlocked.Increment(ref _cleanupCounter) % 1000 == 0)
            {
                foreach (var pair in _locks)
                {
                    if (!pair.Value.TryGetTarget(out _))
                        _locks.TryRemove(pair.Key, out _);
                }
            }
            lock (realLock)
            {
                if (_cache.TryGetValue(key, out value) && value is T cached2)
                    return cached2;
                var created = factory();
                Set(key, created, isList);
                return created;
            }
        }
        // Fallback (should not happen)
        var fallbackLock = new object();
        lock (fallbackLock)
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


