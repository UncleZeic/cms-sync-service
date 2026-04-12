
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Caching.Memory;
using CmsSyncService.Application.Caching;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System;

namespace CmsSyncService.Benchmarks;


[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class EntityCacheServiceBenchmarks
{
    private EntityCacheService _cacheService = null!;
    private string _key = "test_key";
    private string _value = "test_value";
    private string _largeValue = new string('x', 100_000);
    private string _missKey = "missing_key";

    [GlobalSetup]
    public void Setup()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CmsSyncService.Application.CacheDurations { Entity = 5, EntityList = 2 });
        _cacheService = new EntityCacheService(memoryCache, options);
        _cacheService.Set(_key, _value);
        _cacheService.Set("large_key", _largeValue);
    }

    [Benchmark(Baseline = true)]
    public string? Get_Cached_Value()
    {
        return _cacheService.Get<string>(_key);
    }

    [Benchmark]
    public void Set_Value()
    {
        _cacheService.Set(_key, _value);
    }

    [Benchmark]
    public string? Get_Cold_Cache()
    {
        var key = Guid.NewGuid().ToString();
        _cacheService.Set(key, _value);
        return _cacheService.Get<string>(key);
    }

    [Benchmark]
    public string? Get_Large_Object()
    {
        return _cacheService.Get<string>("large_key");
    }

    [Benchmark]
    public string? Get_Miss()
    {
        return _cacheService.Get<string>(_missKey);
    }

    [Benchmark]
    public void Concurrent_Get()
    {
        Parallel.For(0, 10, _ => _cacheService.Get<string>(_key));
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<EntityCacheServiceBenchmarks>();
    }
}
