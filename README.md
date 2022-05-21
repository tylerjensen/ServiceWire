[![tylerjensen](https://circleci.com/gh/tylerjensen/ServiceWire.svg?style=shield)](https://circleci.com/gh/tylerjensen/ServiceWire)  --.NET 6.0 not yet supported.

ServiceWire
===========

### Capture serialization error bug fix in 5.4.2

1. Single target of NetStandard 2.0 for a smaller NuGet package.
2. Fix to a NamedPipes performance issue.
3. Elimination of NET462 code differences.

### Capture serialization error bug fix in 5.4.1

1. In .NET 5+, the BinaryFormatter is marked obsolete and prohibited in ASP.NET apps.
2. This bug caused an end of stream error rather than capturing it properly. This version fixes that bug and exposes the limitation introduced in .NET 5+ on ASP.NET apps.
3. Using ServiceWire in an ASP.NET app is still possible but requires the use of the [EnableUnsafeBinaryFormatterSerialization](https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/binaryformatter-serialization-obsolete) flag in your project file. Use this carefully and be sure you understand the risks. 

### ICompression added for injecting compression strategy in 5.4.0

1. Added ICompression for injecting custom compression into usage.
2. Added .NET 5.0 as target back in.

### AssemblyQualified names from user defined types in 5.3.6

1. Fix for AssemblyQualified names from user defined types.

### Multiple Framework Targets and void Return Types 5.3.5

1. Updated all projects to target .net462, .net48, netcoreapp3.1, and net6.0 only.
2. Corrected multiple targets for multiple OS in projects for those using Linux.
3. Updated NuGet package version. 

### BugFix + Test cases + 48

1. Throwing the original error through an Intercept would fail for interface methods that have a void return type
2. Updated framework references from .net462 to .net48

### .NET Framework to .NET Core and Serializer Bug Fixes 5.3.4

1. Support for .NET Framework to .NET Core core parameter types to eliminate exceptions when a Framework client is talking to a Core host or vice versa. 
2. Serializer injection bug fixed.

### .NET 4.62 added back in version 5.3.3

1. Added .NET Framework 4.62 build in package to prevent permissions issue in named pipes. 
2. Fixed custom serializer issue. 
3. .NET Standard 2.0 and 2.1 builds remain. 
4. Resolved parallel Zk test issues.

Note: Use of async/await and Task<T> not recommended. Use of Task return type not supported. While the syntax of Task return type is supported, apparently it is not marked as Serializable. In fact async/await is not really supported. Under the covers the task type is stripped away over the wire and the method is executed on a worker thread on the server synchronously. If you think about it, you will understand that it's two separate processes, so the Task Parallel Library is not going to be able to manage the thread context across the processes. RPC is inherently synchronous but the handling of each request on the host is done on thread pools.

### .NET Standard 2.0 and 2.1 in version 5.3.2

1. Changed library build to only .NET Standard 2.0 and 2.1.
2. This breaks users of named pipes in .NET 4.6.2 -- DO NOT UPGRADE until we resolve that issue.

### Bug Fixes in version 5.3.1

1. Fixed bug related to complex type serialization that occurred when using output parameters.


### BREAKING CHANGES in version 5.3.0

1. Injectable serialization (see project library tests for examples). 

2. Removes dependency on Newtonsoft.Json and uses BinaryFormatter for default serialization which means wire data classes must be marked [Serializable]. 

3. Internal classes are attributed to support protobuf-net serialization as well.

### Changes in version 5.2.0

1. Adds support for return types of Task and Task<T> to support async / await across the wire.

### Changes including some breaking changes in version 5.1.0

1. Dropped strong named assembly.

2. Support for NetCoreApp 2.0, 2.2 and .NET Framework 4.62. Dropped support for .NET 3.5.

3. Modified projects and NuGet package generation from Visual Studio 2017.

4. Dropped separate projects used to build different targets.

5. Converted test projects to XUnit with multiple targets to allow "dotnet test" run of all targets.


### Breaking Changes in version 4.0.1

1. Switched ServiceWire (and ServiceMq) to Newtonsoft.Json for serialization. Eliminates use of BinaryFormatter and its required Serializable attribute. Also eliminates ServiceStack.Text 3 dependency which has problems serializing structs.

2. Relaxed assembly version matching to allow additive changes without breaking the client or requiring an immediate client update.

3. Strong name added to allow the library to be used by strong named applications and libraries.

4. Added .NET 3.5 support to allow legacy applications to use the library. This adds a Framework specific dependency on TaskParallelLibrary 1.0.2856.0.

5. For the .NET 4.0 and 3.5 versions, changed to "Client Profile" for the target framework.

6. Removed dependency on System.Numerics in order to support .NET 3.5 and introduced ZkBigInt class taken from Scott Garland's BigInteger class. See license text for full attribution.

### A Lightweight Services Library for .NET.

ServiceWire is a very fast and light weight services host and dynamic client library that simplifies the development and use of high performance remote procedure call (RPC) communication between .NET processes over Named Pipes or TCP/IP.

Find "how to use" examples in the tests code. [ServiceWire documentation][] is available on the wiki.

### Important

ServiceWire's dynamically generated proxy will NOT run as x86 on an x64 system. This ususally occurs when you use Visual Studio to create a console application with the default "prefer x86" in project properties selected. Just be sure to choose AnyCPU or the specific target (x86 or x64) so that you do not run 32bit in WOW on an x64 machine.

### Get It on Nuget

Get the [NuGet package here][].

### Using the library is easy. 

1.  Code your interface

2.  Code your implementation

3.  Host the implementation

4.  Use dynamic proxy of your interface on the client side

### This unique library supports:

-   TCP and NamedPipes protocols

-   ByRef (out and ref) parameters (except for non-primitive value types)

-   Dynamic client proxy generation from service interface

-   Very fast serialization of most native types and arrays of those types

-   Multiple service interface hosting on the same endpoint

-   Aspect oriented interception with pre-, post- and exception handling cross cutting

-   Hosting of single service implementation singleton on multiple endpoints and protocols

-   Protocol, serialization and execution strategy extension

Portions of this library are a derivative of [RemotingLite][].  

  [NuGet package here]: http://www.nuget.org/packages/ServiceWire/
  [RemotingLite]: http://remotinglite.codeplex.com/
  [ServiceWire documentation]: https://github.com/tylerjensen/ServiceWire/wiki

### ConnBenchmarks (5/21/2022)

The Conn benchmarks measure the establishment on a host and client connection and one simple operation. These benchmarks summarize the cost of making the connection.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-ICGMVM : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-UECWEW : .NET Framework 4.8 (4.8.4510.0), X64 RyuJIT

InvocationCount=64  MaxIterationCount=16  MinIterationCount=4
UnrollFactor=1

|  Method |        Job |            Runtime |        Mean |     Error |    StdDev | Ratio | RatioSD |   Gen 0 | Allocated |
|-------- |----------- |------------------- |------------:|----------:|----------:|------:|--------:|--------:|----------:|
| TcpConn | Job-ICGMVM |           .NET 6.0 | 15,515.9 us | 419.21 us | 350.06 us |  1.00 |    0.00 |       - |     52 KB |
| TcpConn | Job-UECWEW | .NET Framework 4.8 |  1,960.8 us | 841.09 us | 786.76 us |  0.13 |    0.05 | 15.6250 |     97 KB |
|         |            |                    |             |           |           |       |         |         |           |
|  NpConn | Job-ICGMVM |           .NET 6.0 |    270.9 us |   5.48 us |   5.12 us |  1.00 |    0.00 |       - |     64 KB |
|  NpConn | Job-UECWEW | .NET Framework 4.8 |    297.9 us |   6.50 us |   6.39 us |  1.10 |    0.03 | 15.6250 |    107 KB |
```

### AllBenchmarks (5/21/2022)

The All benchmarks measure operations on named pipes and tcp (localhost) connections after the connection has been established.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-AUNACT : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-NEMODS : .NET Framework 4.8 (4.8.4510.0), X64 RyuJIT

InvocationCount=1024  MaxIterationCount=64  MinIterationCount=8
UnrollFactor=1

|       Method |        Job |            Runtime |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 | Allocated |
|------------- |----------- |------------------- |----------:|---------:|---------:|------:|--------:|--------:|----------:|
|       TcpSim | Job-AUNACT |           .NET 6.0 |  32.10 us | 0.641 us | 0.686 us |  1.00 |    0.00 |       - |     641 B |
|       TcpSim | Job-NEMODS | .NET Framework 4.8 |  33.90 us | 0.609 us | 1.099 us |  1.06 |    0.04 |       - |     856 B |
|              |            |                    |           |          |          |       |         |         |           |
|   TcpSimJson | Job-AUNACT |           .NET 6.0 |  32.10 us | 0.381 us | 0.169 us |  1.00 |    0.00 |       - |     641 B |
|   TcpSimJson | Job-NEMODS | .NET Framework 4.8 |  32.03 us | 0.598 us | 0.560 us |  1.00 |    0.02 |       - |     856 B |
|              |            |                    |           |          |          |       |         |         |           |
|        TcpRg | Job-AUNACT |           .NET 6.0 | 171.08 us | 1.829 us | 0.812 us |  1.00 |    0.00 | 10.7422 |  69,717 B |
|        TcpRg | Job-NEMODS | .NET Framework 4.8 | 228.85 us | 3.442 us | 1.528 us |  1.34 |    0.01 | 12.6953 |  80,716 B |
|              |            |                    |           |          |          |       |         |         |           |
|    TcpRgJson | Job-AUNACT |           .NET 6.0 | 170.89 us | 2.648 us | 1.752 us |  1.00 |    0.00 | 10.7422 |  69,717 B |
|    TcpRgJson | Job-NEMODS | .NET Framework 4.8 | 228.15 us | 4.329 us | 2.863 us |  1.34 |    0.02 | 12.6953 |  80,661 B |
|              |            |                    |           |          |          |       |         |         |           |
|     TcpCxOut | Job-AUNACT |           .NET 6.0 |  81.39 us | 1.405 us | 0.929 us |  1.00 |    0.00 |  2.9297 |  19,797 B |
|     TcpCxOut | Job-NEMODS | .NET Framework 4.8 | 100.65 us | 1.675 us | 1.398 us |  1.24 |    0.03 |  3.9063 |  28,586 B |
|              |            |                    |           |          |          |       |         |         |           |
| TcpCxOutJson | Job-AUNACT |           .NET 6.0 |  82.29 us | 1.642 us | 1.282 us |  1.00 |    0.00 |  2.9297 |  19,797 B |
| TcpCxOutJson | Job-NEMODS | .NET Framework 4.8 | 100.20 us | 1.790 us | 0.936 us |  1.22 |    0.03 |  3.9063 |  28,581 B |
|              |            |                    |           |          |          |       |         |         |           |
|        NpSim | Job-AUNACT |           .NET 6.0 |  23.78 us | 0.471 us | 0.861 us |  1.00 |    0.00 |       - |     641 B |
|        NpSim | Job-NEMODS | .NET Framework 4.8 |  24.04 us | 0.479 us | 0.606 us |  1.01 |    0.04 |       - |   1,008 B |
|              |            |                    |           |          |          |       |         |         |           |
|    NpSimJson | Job-AUNACT |           .NET 6.0 |  23.92 us | 0.462 us | 0.692 us |  1.00 |    0.00 |       - |     641 B |
|    NpSimJson | Job-NEMODS | .NET Framework 4.8 |  24.05 us | 0.477 us | 0.848 us |  1.01 |    0.06 |       - |   1,016 B |
|              |            |                    |           |          |          |       |         |         |           |
|         NpRg | Job-AUNACT |           .NET 6.0 | 162.42 us | 2.800 us | 1.243 us |  1.00 |    0.00 | 10.7422 |  69,717 B |
|         NpRg | Job-NEMODS | .NET Framework 4.8 | 222.95 us | 4.147 us | 3.676 us |  1.37 |    0.02 | 12.6953 |  80,721 B |
|              |            |                    |           |          |          |       |         |         |           |
|     NpRgJson | Job-AUNACT |           .NET 6.0 |  85.35 us | 1.570 us | 0.934 us |  1.00 |    0.00 |  3.9063 |  27,211 B |
|     NpRgJson | Job-NEMODS | .NET Framework 4.8 | 109.36 us | 1.925 us | 2.503 us |  1.30 |    0.04 |  4.8828 |  36,339 B |
|              |            |                    |           |          |          |       |         |         |           |
|      NpCxOut | Job-AUNACT |           .NET 6.0 |  74.25 us | 1.448 us | 1.047 us |  1.00 |    0.00 |  2.9297 |  19,797 B |
|      NpCxOut | Job-NEMODS | .NET Framework 4.8 |  95.20 us | 1.547 us | 1.119 us |  1.28 |    0.02 |  3.9063 |  28,991 B |
|              |            |                    |           |          |          |       |         |         |           |
|  NpCxOutJson | Job-AUNACT |           .NET 6.0 |  63.50 us | 1.084 us | 1.249 us |  1.00 |    0.00 |  0.9766 |  10,026 B |
|  NpCxOutJson | Job-NEMODS | .NET Framework 4.8 |  75.30 us | 1.045 us | 0.464 us |  1.17 |    0.02 |  1.9531 |  17,807 B |
```

### Initial Benchmark (5/22/2022)

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]             : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  .NET 6.0           : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  .NET Framework 4.8 : .NET Framework 4.8 (4.8.4510.0), X64 RyuJIT


|      Method |                Job |            Runtime |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Allocated |
|------------ |------------------- |------------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|
|       NpSim |           .NET 6.0 |           .NET 6.0 |  23.11 us | 0.168 us | 0.157 us |  1.00 |    0.00 |  0.0916 |      - |     712 B |
|       NpSim | .NET Framework 4.8 | .NET Framework 4.8 |  24.07 us | 0.328 us | 0.307 us |  1.04 |    0.01 |  0.1831 |      - |   1,290 B |
|             |                    |                    |           |          |          |       |         |         |        |           |
|   NpSimJson |           .NET 6.0 |           .NET 6.0 |  23.68 us | 0.220 us | 0.195 us |  1.00 |    0.00 |  0.0916 |      - |     712 B |
|   NpSimJson | .NET Framework 4.8 | .NET Framework 4.8 |  24.13 us | 0.133 us | 0.124 us |  1.02 |    0.01 |  0.1831 |      - |   1,290 B |
|             |                    |                    |           |          |          |       |         |         |        |           |
|        NpRg |           .NET 6.0 |           .NET 6.0 | 159.31 us | 1.656 us | 1.468 us |  1.00 |    0.00 | 11.2305 | 0.4883 |  69,715 B |
|        NpRg | .NET Framework 4.8 | .NET Framework 4.8 | 218.43 us | 3.239 us | 3.030 us |  1.37 |    0.02 | 12.6953 | 0.4883 |  80,672 B |
|             |                    |                    |           |          |          |       |         |         |        |           |
|    NpRgJson |           .NET 6.0 |           .NET 6.0 |  85.15 us | 1.347 us | 1.194 us |  1.00 |    0.00 |  4.5166 |      - |  27,209 B |
|    NpRgJson | .NET Framework 4.8 | .NET Framework 4.8 | 111.02 us | 1.189 us | 1.112 us |  1.30 |    0.03 |  5.6152 |      - |  36,343 B |
|             |                    |                    |           |          |          |       |         |         |        |           |
|     NpCxOut |           .NET 6.0 |           .NET 6.0 |  74.16 us | 0.723 us | 0.604 us |  1.00 |    0.00 |  3.1738 |      - |  19,795 B |
|     NpCxOut | .NET Framework 4.8 | .NET Framework 4.8 |  95.84 us | 1.042 us | 0.974 us |  1.29 |    0.02 |  4.5166 |      - |  28,984 B |
|             |                    |                    |           |          |          |       |         |         |        |           |
| NpCxOutJson |           .NET 6.0 |           .NET 6.0 |  63.58 us | 0.459 us | 0.430 us |  1.00 |    0.00 |  1.5869 |      - |  10,025 B |
| NpCxOutJson | .NET Framework 4.8 | .NET Framework 4.8 |  76.82 us | 0.769 us | 0.682 us |  1.21 |    0.01 |  2.8076 |      - |  17,796 B |
```