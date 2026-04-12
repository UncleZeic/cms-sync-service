using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;
using CmsSyncService.Application.Caching;
using Microsoft.Extensions.Options;

namespace CmsSyncService.Benchmarks;

public class EntityCacheServiceBenchmarks
{
    private EntityCacheService _cacheService = null!;
    private string _key = "test_key";
    private string _value = "test_value";

    [GlobalSetup]
    public void Setup()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CmsSyncService.Application.CacheDurations { Entity = 5, EntityList = 2 });
        _cacheService = new EntityCacheService(memoryCache, options);
        _cacheService.Set(_key, _value);
    }

    [Benchmark]
    public string? Get_Cached_Value()
    {
        return _cacheService.Get<string>(_key);
    }

    [Benchmark]
    public void Set_Value()
    {
        _cacheService.Set(_key, _value);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<EntityCacheServiceBenchmarks>();
    }
}
