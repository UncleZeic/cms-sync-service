
using CmsSyncService.Application;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace CmsSyncService.Application.Caching;


public class EntityCacheService : IEntityCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly int _entityCacheMinutes;
    private readonly int _entityListCacheMinutes;

    public EntityCacheService(IDistributedCache cache, IOptions<CacheDurations> options)
    {
        _cache = cache;
        _entityCacheMinutes = options.Value.Entity;
        _entityListCacheMinutes = options.Value.EntityList;
    }
    public T? Get<T>(string key) where T : class
    {
        var value = _cache.GetString(key);
        return value is null ? null : JsonSerializer.Deserialize<T>(value, SerializerOptions);
    }

    private static readonly ConcurrentDictionary<string, CacheKeyLock> _locks = new();

    public T? GetOrCreate<T>(string key, Func<T> factory, bool isList = false) where T : class
    {
        var cached = Get<T>(key);
        if (cached is not null)
            return cached;

        var keyLock = AcquireLock(key);
        keyLock.Semaphore.Wait();
        try
        {
            var cached2 = Get<T>(key);
            if (cached2 is not null)
                return cached2;

            var created = factory();
            Set(key, created, isList);
            return created;
        }
        finally
        {
            keyLock.Semaphore.Release();
            ReleaseLock(key, keyLock);
        }
    }

    public void Set<T>(string key, T value) where T : class => Set(key, value, false);

    private void Set<T>(string key, T value, bool isList) where T : class
    {
        var options = new DistributedCacheEntryOptions();
        if (isList ||
            key == EntityCacheKeys.GetEntityListKey(true) ||
            key == EntityCacheKeys.GetEntityListKey(false) ||
            key == EntityCacheKeys.GetDefaultPagedEntityListKey(true) ||
            key == EntityCacheKeys.GetDefaultPagedEntityListKey(false))
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_entityListCacheMinutes);
            options.SlidingExpiration = TimeSpan.FromMinutes(_entityListCacheMinutes / 2.0);
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_entityCacheMinutes);
            options.SlidingExpiration = TimeSpan.FromMinutes(_entityCacheMinutes / 2.0);
        }
        _cache.SetString(key, JsonSerializer.Serialize(value, SerializerOptions), options);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    private static CacheKeyLock AcquireLock(string key)
    {
        while (true)
        {
            var keyLock = _locks.GetOrAdd(key, _ => new CacheKeyLock());

            lock (keyLock.SyncRoot)
            {
                if (!_locks.TryGetValue(key, out var currentLock) ||
                    !ReferenceEquals(currentLock, keyLock) ||
                    keyLock.IsRetired)
                {
                    continue;
                }

                keyLock.ReferenceCount++;
                return keyLock;
            }
        }
    }

    private static void ReleaseLock(string key, CacheKeyLock keyLock)
    {
        var shouldDispose = false;

        lock (keyLock.SyncRoot)
        {
            keyLock.ReferenceCount--;
            if (keyLock.ReferenceCount == 0 &&
                _locks.TryRemove(new KeyValuePair<string, CacheKeyLock>(key, keyLock)))
            {
                keyLock.IsRetired = true;
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            keyLock.Semaphore.Dispose();
        }
    }

    private sealed class CacheKeyLock
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public object SyncRoot { get; } = new();
        public int ReferenceCount { get; set; }
        public bool IsRetired { get; set; }
    }
}

public interface IEntityCacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value) where T : class;
    void Remove(string key);
    T? GetOrCreate<T>(string key, Func<T> factory, bool isList = false) where T : class;
}
