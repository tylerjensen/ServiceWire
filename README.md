[![tylerjensen](https://circleci.com/gh/tylerjensen/ServiceWire.svg?style=shield)](https://circleci.com/gh/tylerjensen/ServiceWire)  --.NET 6.0 not yet supported.

ServiceWire
===========

### Replaces BinaryFormatter with System.Text.Json 5.5.0

1. Replaces BinaryFormatter in DefaultSerializer with System.Text.Json. Improves performance and reduces allocations in serializing small object graphs which is the most common use case in any RPC library.
2. Fixes null value in string array bug #50. 
3. See source for former DefaultSerializer in ServiceWire.Serializers in BinaryFormatterSerializer. Use that code as a custom injected serializer if this version breaks your serialization.
4. Using ServiceWire in an ASP.NET app no longer requires the use of the EnableUnsafeBinaryFormatterSerialization flag in your project file.

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


### ConnBenchmarks (6/6/2022)

No real change from previous benchmarks. Making a connection is still expensive. About 15ms.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-OBEYHZ : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-REVQDN : .NET Framework 4.8 (4.8.4510.0), X64 RyuJIT

InvocationCount=64  MaxIterationCount=16  MinIterationCount=4
UnrollFactor=1

|  Method |        Job |            Runtime |        Mean |     Error |    StdDev | Ratio | RatioSD |   Gen 0 | Allocated |
|-------- |----------- |------------------- |------------:|----------:|----------:|------:|--------:|--------:|----------:|
| TcpConn | Job-OBEYHZ |           .NET 6.0 | 15,225.3 us | 314.91 us | 294.56 us |  1.00 |    0.00 |       - |     52 KB |
| TcpConn | Job-REVQDN | .NET Framework 4.8 |    736.5 us |  62.61 us |  55.50 us |  0.05 |    0.00 | 15.6250 |     96 KB |
|         |            |                    |             |           |           |       |         |         |           |
|  NpConn | Job-OBEYHZ |           .NET 6.0 |    265.1 us |   8.23 us |   8.08 us |  1.00 |    0.00 |       - |     63 KB |
|  NpConn | Job-REVQDN | .NET Framework 4.8 |    307.5 us |  16.52 us |  16.23 us |  1.16 |    0.07 | 15.6250 |    107 KB |
```

### AllBenchmarks (6/6/2022)

These benchmarks so the improvements in switching to System.Text.Json in the CxOut tests. Faster and fewer allocations that previous benchmarks. Performance improvement is 17% on average on those specific benchmarks.

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-NJJOJB : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  Job-VIKKWS : .NET Framework 4.8 (4.8.4510.0), X64 RyuJIT

InvocationCount=1024  MaxIterationCount=64  MinIterationCount=8
UnrollFactor=1

|       Method |        Job |            Runtime |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Allocated |
|------------- |----------- |------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|
|       TcpSim | Job-NJJOJB |           .NET 6.0 |  32.93 us | 0.626 us | 0.696 us |  1.00 |    0.00 |      - |     569 B |
|       TcpSim | Job-VIKKWS | .NET Framework 4.8 |  34.36 us | 0.661 us | 0.904 us |  1.04 |    0.04 |      - |     776 B |
|              |            |                    |           |          |          |       |         |        |           |
|   TcpSimJson | Job-NJJOJB |           .NET 6.0 |  31.79 us | 0.635 us | 0.680 us |  1.00 |    0.00 |      - |     569 B |
|   TcpSimJson | Job-VIKKWS | .NET Framework 4.8 |  32.46 us | 0.641 us | 0.536 us |  1.02 |    0.03 |      - |     784 B |
|              |            |                    |           |          |          |       |         |        |           |
|        TcpRg | Job-NJJOJB |           .NET 6.0 |  88.75 us | 1.625 us | 1.269 us |  1.00 |    0.00 | 1.9531 |  15,298 B |
|        TcpRg | Job-VIKKWS | .NET Framework 4.8 | 123.68 us | 2.293 us | 1.199 us |  1.39 |    0.03 | 3.9063 |  24,682 B |
|              |            |                    |           |          |          |       |         |        |           |
|    TcpRgJson | Job-NJJOJB |           .NET 6.0 |  90.42 us | 1.683 us | 1.217 us |  1.00 |    0.00 | 1.9531 |  15,298 B |
|    TcpRgJson | Job-VIKKWS | .NET Framework 4.8 | 138.37 us | 3.860 us | 8.791 us |  1.50 |    0.13 | 3.9063 |  24,671 B |
|              |            |                    |           |          |          |       |         |        |           |
|     TcpCxOut | Job-NJJOJB |           .NET 6.0 |  68.30 us | 1.305 us | 1.019 us |  1.00 |    0.00 | 0.9766 |   6,810 B |
|     TcpCxOut | Job-VIKKWS | .NET Framework 4.8 |  92.20 us | 2.251 us | 5.081 us |  1.28 |    0.04 | 1.9531 |  13,910 B |
|              |            |                    |           |          |          |       |         |        |           |
| TcpCxOutJson | Job-NJJOJB |           .NET 6.0 |  69.14 us | 1.319 us | 1.234 us |  1.00 |    0.00 | 0.9766 |   6,810 B |
| TcpCxOutJson | Job-VIKKWS | .NET Framework 4.8 |  86.90 us | 1.424 us | 1.949 us |  1.27 |    0.04 | 1.9531 |  13,908 B |
|              |            |                    |           |          |          |       |         |        |           |
|        NpSim | Job-NJJOJB |           .NET 6.0 |  23.71 us | 0.469 us | 0.716 us |  1.00 |    0.00 |      - |     569 B |
|        NpSim | Job-VIKKWS | .NET Framework 4.8 |  25.19 us | 0.485 us | 0.519 us |  1.06 |    0.04 |      - |     936 B |
|              |            |                    |           |          |          |       |         |        |           |
|    NpSimJson | Job-NJJOJB |           .NET 6.0 |  24.02 us | 0.477 us | 0.783 us |  1.00 |    0.00 |      - |     569 B |
|    NpSimJson | Job-VIKKWS | .NET Framework 4.8 |  27.08 us | 0.540 us | 0.887 us |  1.13 |    0.05 |      - |     936 B |
|              |            |                    |           |          |          |       |         |        |           |
|         NpRg | Job-NJJOJB |           .NET 6.0 |  76.97 us | 1.453 us | 0.961 us |  1.00 |    0.00 | 1.9531 |  15,298 B |
|         NpRg | Job-VIKKWS | .NET Framework 4.8 | 107.98 us | 2.029 us | 1.342 us |  1.40 |    0.03 | 3.9063 |  24,698 B |
|              |            |                    |           |          |          |       |         |        |           |
|     NpRgJson | Job-NJJOJB |           .NET 6.0 |  86.54 us | 1.679 us | 0.999 us |  1.00 |    0.00 | 3.9063 |  27,139 B |
|     NpRgJson | Job-VIKKWS | .NET Framework 4.8 | 116.80 us | 2.208 us | 4.846 us |  1.35 |    0.06 | 4.8828 |  36,338 B |
|              |            |                    |           |          |          |       |         |        |           |
|      NpCxOut | Job-NJJOJB |           .NET 6.0 |  60.93 us | 1.180 us | 1.312 us |  1.00 |    0.00 | 0.9766 |   6,810 B |
|      NpCxOut | Job-VIKKWS | .NET Framework 4.8 |  82.04 us | 1.506 us | 2.715 us |  1.36 |    0.05 | 1.9531 |  14,211 B |
|              |            |                    |           |          |          |       |         |        |           |
|  NpCxOutJson | Job-NJJOJB |           .NET 6.0 |  66.24 us | 1.223 us | 0.955 us |  1.00 |    0.00 | 0.9766 |  10,858 B |
|  NpCxOutJson | Job-VIKKWS | .NET Framework 4.8 |  82.45 us | 1.641 us | 2.133 us |  1.27 |    0.04 | 2.9297 |  18,729 B |
```
