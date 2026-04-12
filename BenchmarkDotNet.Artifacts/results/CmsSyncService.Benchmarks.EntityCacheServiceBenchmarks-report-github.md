```

BenchmarkDotNet v0.13.12, macOS 15.7.1 (24G231) [Darwin 24.6.0]
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2


```
| Method           | Mean     | Error   | StdDev   |
|----------------- |---------:|--------:|---------:|
| Get_Cached_Value | 101.6 ns | 2.11 ns |  3.35 ns |
| Set_Value        | 272.6 ns | 5.50 ns | 10.46 ns |
