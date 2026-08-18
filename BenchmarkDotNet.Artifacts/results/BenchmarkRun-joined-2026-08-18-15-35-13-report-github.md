```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen Threadripper PRO 5975WX 32-Cores, 1 CPU, 64 logical and 32 physical cores
.NET SDK 10.0.204
  [Host]   : .NET 8.0.30 (8.0.3026.36720), X64 RyuJIT AVX2
  .NET 6.0 : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.30 (8.0.3026.36720), X64 RyuJIT AVX2


```
| Type                 | Method       | Job                | Runtime            | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |------------- |------------------- |------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ConnectionBenchmarks | TcpConn      | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| ConnectionBenchmarks | TcpConn      | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| ConnectionBenchmarks | TcpConn      | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpSim        | .NET 6.0           | .NET 6.0           |  14.94 μs |  0.298 μs |  0.879 μs |  15.22 μs |  1.05 |    0.08 | 0.0153 |      - |     320 B |        1.00 |
| NamedPipesBenchmarks | NpSim        | .NET 8.0           | .NET 8.0           |  14.20 μs |  0.281 μs |  0.761 μs |  14.29 μs |  1.00 |    0.08 | 0.0153 |      - |     320 B |        1.00 |
| NamedPipesBenchmarks | NpSim        | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpSim       | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpSim       | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpSim       | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| ConnectionBenchmarks | NpConn       | .NET 6.0           | .NET 6.0           | 664.58 μs | 11.618 μs | 10.299 μs | 661.95 μs |  1.07 |    0.02 | 4.8828 | 1.9531 |   71887 B |        1.01 |
| ConnectionBenchmarks | NpConn       | .NET 8.0           | .NET 8.0           | 620.45 μs | 10.063 μs |  8.403 μs | 623.43 μs |  1.00 |    0.02 | 4.8828 |      - |   71115 B |        1.00 |
| ConnectionBenchmarks | NpConn       | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpSimJson    | .NET 6.0           | .NET 6.0           |  14.79 μs |  0.294 μs |  0.789 μs |  14.67 μs |  1.02 |    0.07 |      - |      - |     320 B |        1.00 |
| NamedPipesBenchmarks | NpSimJson    | .NET 8.0           | .NET 8.0           |  14.52 μs |  0.288 μs |  0.520 μs |  14.61 μs |  1.00 |    0.05 | 0.0153 |      - |     320 B |        1.00 |
| NamedPipesBenchmarks | NpSimJson    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpSimJson   | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpSimJson   | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpSimJson   | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpRg         | .NET 6.0           | .NET 6.0           |  26.78 μs |  0.688 μs |  2.029 μs |  27.71 μs |  1.26 |    0.15 | 0.5798 |      - |    9744 B |        1.29 |
| NamedPipesBenchmarks | NpRg         | .NET 8.0           | .NET 8.0           |  21.44 μs |  0.632 μs |  1.863 μs |  22.24 μs |  1.01 |    0.13 | 0.4578 |      - |    7528 B |        1.00 |
| NamedPipesBenchmarks | NpRg         | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpRg        | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpRg        | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpRg        | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpRgJson     | .NET 6.0           | .NET 6.0           |  35.74 μs |  1.018 μs |  3.001 μs |  36.60 μs |  1.19 |    0.13 | 1.2817 |      - |   21680 B |        1.19 |
| NamedPipesBenchmarks | NpRgJson     | .NET 8.0           | .NET 8.0           |  30.24 μs |  0.685 μs |  2.018 μs |  30.23 μs |  1.00 |    0.09 | 1.0986 |      - |   18184 B |        1.00 |
| NamedPipesBenchmarks | NpRgJson     | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpRgJson    | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpRgJson    | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpRgJson    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpCxOut      | .NET 6.0           | .NET 6.0           |  25.68 μs |  0.835 μs |  2.462 μs |  26.66 μs |  1.03 |    0.25 | 0.1526 |      - |    2760 B |        1.17 |
| NamedPipesBenchmarks | NpCxOut      | .NET 8.0           | .NET 8.0           |  26.44 μs |  2.205 μs |  6.501 μs |  23.61 μs |  1.06 |    0.36 | 0.1221 |      - |    2352 B |        1.00 |
| NamedPipesBenchmarks | NpCxOut      | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpCxOut     | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpCxOut     | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpCxOut     | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpCxOutJson  | .NET 6.0           | .NET 6.0           |  29.74 μs |  1.029 μs |  3.034 μs |  30.44 μs |  0.73 |    0.09 | 0.3662 |      - |    6688 B |        1.03 |
| NamedPipesBenchmarks | NpCxOutJson  | .NET 8.0           | .NET 8.0           |  40.74 μs |  0.960 μs |  2.830 μs |  42.34 μs |  1.01 |    0.10 | 0.3662 |      - |    6520 B |        1.00 |
| NamedPipesBenchmarks | NpCxOutJson  | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
|                      |              |                    |                    |           |           |           |           |       |         |        |        |           |             |
| TcpBenchmarks        | TcpCxOutJson | .NET 6.0           | .NET 6.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpCxOutJson | .NET 8.0           | .NET 8.0           |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |
| TcpBenchmarks        | TcpCxOutJson | .NET Framework 4.8 | .NET Framework 4.8 |        NA |        NA |        NA |        NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
  ConnectionBenchmarks.TcpConn: .NET 6.0(Runtime=.NET 6.0)
  ConnectionBenchmarks.TcpConn: .NET 8.0(Runtime=.NET 8.0)
  ConnectionBenchmarks.TcpConn: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpSim: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpSim: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpSim: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpSim: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  ConnectionBenchmarks.NpConn: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpSimJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpSimJson: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpSimJson: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpSimJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpRg: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpRg: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpRg: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpRg: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpRgJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpRgJson: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpRgJson: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpRgJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpCxOut: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpCxOut: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpCxOut: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpCxOut: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  NamedPipesBenchmarks.NpCxOutJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TcpBenchmarks.TcpCxOutJson: .NET 6.0(Runtime=.NET 6.0)
  TcpBenchmarks.TcpCxOutJson: .NET 8.0(Runtime=.NET 8.0)
  TcpBenchmarks.TcpCxOutJson: .NET Framework 4.8(Runtime=.NET Framework 4.8)
