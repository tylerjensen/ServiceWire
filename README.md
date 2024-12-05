[![.NET](https://github.com/tylerjensen/ServiceWire/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/tylerjensen/ServiceWire/actions/workflows/build-and-test.yml) 

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/tylerjensen)

ServiceWire
===========

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

Portions of this library (dynamic proxy) are a derivative of RemotingLite by Frank Thomsen.  

  [NuGet package here]: http://www.nuget.org/packages/ServiceWire/
  [RemotingLite by Frank Thomsen]: https://codeplexarchive.org/codeplex/project/RemotingLite
  [ServiceWire documentation]: https://github.com/tylerjensen/ServiceWire/wiki

## History

### NamedPipeServerStreamFactory and Other Improvements 5.6.0

1. Contributed fix where accepting TCP clients synchronously may block new clients from being accepted until the terminating request is received on the synchronous client.
1. Contributed NamedPipeServerStreamFactory to allow greater level of permissions control in using named pipes.
1. Introducted injectable ILog and IStats across channels and clients with default NullLogger and NullStats, making InjectLoggerStats obsolete.
1. Code improvements for code consistency and eliminating outdated frameworks from tests and supporting projects.
1. Updated several dependencies in supporting projects.
1. Updated System.Text.Json to 9.0.0 to resolve known vulnerabilities in previous versions.

### Support for Enum by Ref 5.5.4

1. Contributed support for proper async exceptions.

### Support for Enum by Ref 5.5.3

1. Contributed support for Enum by ref parameters.

### Bug Fix for Important Edge Case 5.5.2

1. Contributed fix to case service on a host with same interface was called previously on a different host.

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

### Updated Benchmarks (12/5/2024) with latest contribution

NOTE: In this and previous runs of the benchmarks, .NET 8 is consistently 21% faster than .NET 6 when the benchmark differences are averaged.

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4460/23H2/2023Update/SunValley3)
AMD Ryzen Threadripper PRO 5975WX 32-Cores, 1 CPU, 64 logical and 32 physical cores
.NET SDK 9.0.100
  [Host]   : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  .NET 6.0 : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


| Type                 | Method       | Job                | Runtime            | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |------------- |------------------- |------------------- |-------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ConnectionBenchmarks | TcpConn      | .NET 6.0           | .NET 6.0           | 15,400.91 us | 204.477 us | 191.267 us |  0.99 |    0.01 |      - |      - |   62874 B |        1.00 |
| ConnectionBenchmarks | TcpConn      | .NET 8.0           | .NET 8.0           | 15,505.96 us |  76.118 us |  71.201 us |  1.00 |    0.01 |      - |      - |   62893 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpSim        | .NET 6.0           | .NET 6.0           |     21.64 us |   0.132 us |   0.110 us |  1.37 |    0.04 | 0.0305 |      - |     568 B |        0.89 |
| NamedPipesBenchmarks | NpSim        | .NET 8.0           | .NET 8.0           |     15.86 us |   0.311 us |   0.415 us |  1.00 |    0.04 | 0.0305 |      - |     640 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpSim       | .NET 6.0           | .NET 6.0           |     27.60 us |   0.215 us |   0.201 us |  1.11 |    0.02 | 0.0305 |      - |     568 B |        0.89 |
| TcpBenchmarks        | TcpSim       | .NET 8.0           | .NET 8.0           |     24.94 us |   0.484 us |   0.497 us |  1.00 |    0.03 | 0.0305 |      - |     640 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| ConnectionBenchmarks | NpConn       | .NET 6.0           | .NET 6.0           |    235.46 us |   3.600 us |   3.192 us |  1.16 |    0.03 | 4.3945 | 0.4883 |   68798 B |        1.02 |
| ConnectionBenchmarks | NpConn       | .NET 8.0           | .NET 8.0           |    203.27 us |   3.865 us |   3.969 us |  1.00 |    0.03 | 4.3945 | 0.4883 |   67196 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpSimJson    | .NET 6.0           | .NET 6.0           |     21.44 us |   0.150 us |   0.125 us |  1.35 |    0.02 | 0.0305 |      - |     568 B |        0.89 |
| NamedPipesBenchmarks | NpSimJson    | .NET 8.0           | .NET 8.0           |     15.94 us |   0.306 us |   0.286 us |  1.00 |    0.02 | 0.0305 |      - |     640 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpSimJson   | .NET 6.0           | .NET 6.0           |     27.80 us |   0.550 us |   0.564 us |  1.14 |    0.03 | 0.0305 |      - |     568 B |        0.89 |
| TcpBenchmarks        | TcpSimJson   | .NET 8.0           | .NET 8.0           |     24.44 us |   0.299 us |   0.280 us |  1.00 |    0.02 | 0.0305 |      - |     640 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpRg         | .NET 6.0           | .NET 6.0           |     61.25 us |   0.710 us |   0.554 us |  1.38 |    0.03 | 0.8545 |      - |   15416 B |        1.05 |
| NamedPipesBenchmarks | NpRg         | .NET 8.0           | .NET 8.0           |     44.48 us |   0.800 us |   1.122 us |  1.00 |    0.03 | 0.9766 |      - |   14737 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpRg        | .NET 6.0           | .NET 6.0           |     72.16 us |   1.325 us |   1.174 us |  1.37 |    0.03 | 0.8545 |      - |   15416 B |        1.05 |
| TcpBenchmarks        | TcpRg        | .NET 8.0           | .NET 8.0           |     52.70 us |   0.834 us |   0.739 us |  1.00 |    0.02 | 0.7324 |      - |   14737 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpRgJson     | .NET 6.0           | .NET 6.0           |     68.63 us |   0.631 us |   0.590 us |  1.39 |    0.04 | 1.7090 |      - |   27193 B |        1.09 |
| NamedPipesBenchmarks | NpRgJson     | .NET 8.0           | .NET 8.0           |     49.40 us |   0.979 us |   1.239 us |  1.00 |    0.03 | 1.4648 |      - |   24881 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpRgJson    | .NET 6.0           | .NET 6.0           |     73.17 us |   1.181 us |   0.986 us |  1.38 |    0.03 | 0.8545 |      - |   15416 B |        1.05 |
| TcpBenchmarks        | TcpRgJson    | .NET 8.0           | .NET 8.0           |     53.03 us |   0.824 us |   0.771 us |  1.00 |    0.02 | 0.7324 |      - |   14737 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpCxOut      | .NET 6.0           | .NET 6.0           |     54.18 us |   0.444 us |   0.416 us |  1.25 |    0.02 | 0.3662 |      - |    6928 B |        0.86 |
| NamedPipesBenchmarks | NpCxOut      | .NET 8.0           | .NET 8.0           |     43.36 us |   0.608 us |   0.569 us |  1.00 |    0.02 | 0.3662 |      - |    8064 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpCxOut     | .NET 6.0           | .NET 6.0           |     64.20 us |   1.172 us |   1.565 us |  1.36 |    0.04 | 0.3662 |      - |    6929 B |        0.86 |
| TcpBenchmarks        | TcpCxOut     | .NET 8.0           | .NET 8.0           |     47.21 us |   0.782 us |   0.732 us |  1.00 |    0.02 | 0.3662 |      - |    8064 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| NamedPipesBenchmarks | NpCxOutJson  | .NET 6.0           | .NET 6.0           |     58.34 us |   0.904 us |   0.801 us |  1.34 |    0.02 | 0.6104 |      - |   10856 B |        0.91 |
| NamedPipesBenchmarks | NpCxOutJson  | .NET 8.0           | .NET 8.0           |     43.61 us |   0.655 us |   0.581 us |  1.00 |    0.02 | 0.7324 |      - |   11888 B |        1.00 |
|                      |              |                    |                    |              |            |            |       |         |        |        |           |             |
| TcpBenchmarks        | TcpCxOutJson | .NET 6.0           | .NET 6.0           |     63.73 us |   1.143 us |   1.069 us |  1.34 |    0.03 | 0.3662 |      - |    6929 B |        0.86 |
| TcpBenchmarks        | TcpCxOutJson | .NET 8.0           | .NET 8.0           |     47.49 us |   0.641 us |   0.600 us |  1.00 |    0.02 | 0.3662 |      - |    8064 B |        1.00 |
```
