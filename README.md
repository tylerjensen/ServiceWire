[![.NET](https://github.com/tylerjensen/ServiceWire/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/tylerjensen/ServiceWire/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/ServiceWire.svg)](https://www.nuget.org/packages/ServiceWire/)

ServiceWire
===========

### A lightweight, very fast RPC library for .NET

ServiceWire lets one .NET process call an interface implemented in another .NET process, over **named pipes** or **TCP/IP**, as if it were local. You write a plain C# interface and implement it once. There is no IDL, no code generator, no attributes and no build step — the client proxy is emitted at runtime from the interface you already have.

![How a call travels from the client proxy to your singleton](https://raw.githubusercontent.com/tylerjensen/ServiceWire/master/docs/images/architecture.svg)

## 📖 [Read the User Guide →](docs/user-guide.md)

Full documentation with worked samples: [getting started](docs/getting-started.md) · [contracts](docs/contracts.md) · [transports](docs/transports.md) · [serialization](docs/serialization.md) · [security](docs/security.md) · [logging](docs/observability.md) · [interception](docs/interception.md) · [wire protocol](docs/wire-protocol.md) · [performance](docs/performance.md) · [migrating to 7.0](docs/migrating-to-v7.md) · [troubleshooting](docs/troubleshooting.md)

<sub>Reading this on NuGet? The guide is at <https://github.com/tylerjensen/ServiceWire/blob/master/docs/user-guide.md></sub>

---

### Install

```shell
dotnet add package ServiceWire
```

Targets `netstandard2.0` and `net8.0`. [NuGet package](http://www.nuget.org/packages/ServiceWire/).

### Use it

```csharp
// 1. A contract both processes reference
public interface IMath
{
    int Add(int a, int b);
}

// 2. An implementation, hosted as a singleton
public class MathService : IMath
{
    public int Add(int a, int b) => a + b;
}

// 3. A host
using var host = new TcpHost(8098);
host.AddService<IMath>(new MathService());
host.Open();

// 4. A client
using var client = new TcpClient<IMath>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, 8098)));
int sum = client.Proxy.Add(2, 3);   // 5
```

Swap `TcpHost`/`TcpClient` for `NpHost`/`NpClient` and the same code runs over a named pipe.

Step-by-step, including the project layout and how to run it: **[Getting started](docs/getting-started.md)**.

### ⚠️ Build for AnyCPU or x64

ServiceWire's dynamically generated proxy will **not** run as x86 on an x64 system. This usually bites when Visual Studio's console template leaves *Prefer 32-bit* enabled. Choose AnyCPU or the specific target so you do not run 32-bit under WOW64 on an x64 machine.

### What it supports

- TCP and named pipe transports, with the same contract on both
- Dynamic client proxy generation from a service interface — no codegen step
- `out` and `ref` parameters (except non-primitive value types)
- Very fast direct encoding of common types and arrays of them
- Multiple service interfaces on one endpoint, and one implementation on multiple endpoints
- Pluggable serialization ([`ISerializer`](docs/serialization.md)) and compression ([`ICompressor`](docs/serialization.md#custom-compression))
- Pluggable logging and timing ([`ILog`, `IStats`](docs/observability.md))
- Optional zero-knowledge authentication with an encrypted session over TCP ([details and caveats](docs/security.md))
- Aspect-oriented [interception](docs/interception.md) with pre-, post- and exception handling
- Concurrent in-flight calls on a shared TCP proxy (7.0)

### Host status

`TcpHost` and `NpHost` expose their listener lifecycle through the inherited `Status` property: `Created`, `Opening`, `Open`, `Faulted`, `Closed`. This reports listener state, not the state of an individual client connection.

```csharp
if (host.Status == HostStatus.Faulted)
{
    // Create and open a replacement host, or otherwise recover the listener.
}
```

See [Logging and diagnostics](docs/observability.md#is-the-host-still-listening) for a supervisor pattern.

---

Portions of this library (the dynamic proxy) are derived from [RemotingLite by Frank Thomsen][]. Licensed under the terms in [License.txt](src/License.txt).

The older [project wiki][] is kept for historical reference; the [user guide](docs/user-guide.md) supersedes it.

  [RemotingLite by Frank Thomsen]: https://codeplexarchive.org/codeplex/project/RemotingLite
  [project wiki]: https://github.com/tylerjensen/ServiceWire/wiki

---

# History

### Performance Release with Negotiated Wire Protocol v2 — 7.0.0

A major performance release. Steady-state calls are **43–70% faster** than 6.0.1 on named pipes and **13–47% faster** on TCP; TCP connection setup is roughly **14× faster** on .NET 8 and 10. Full tables: [Performance](docs/performance.md).

Both mixed-version pairings — 6.x client with 7.0 server, and 7.0 client with 6.x server — keep working over the classic v1 wire path. The new v2 wire activates only when both ends are 7.0, so a fleet can be upgraded in any order. See [Migrating to 7.0](docs/migrating-to-v7.md).

**Internal optimizations** (v1 wire format unchanged, proven byte-identical by golden tests):

1. Memoized type-to-config-name resolution in both directions, removing three regex passes per complex parameter per call and repeated `Type.GetType` lookups in `DefaultSerializer`.
2. Fixed the named-pipe client to actually use its `BufferedStream`, collapsing dozens of per-field pipe syscalls per call into one (and fixing a pipe stream that was never disposed).
3. Set `Socket.NoDelay` on client and accepted sockets to eliminate Nagle and delayed-ACK latency; both sides already buffer and flush once per message.
4. Server dispatch through compiled expression delegates instead of `MethodInfo.Invoke` (byref methods fall back to reflection); async results through compiled `Task.Result` getters; client `Task.FromResult` wrapping through compiled converters. Client-visible exception behavior is unchanged and covered by parity tests.
5. Proxy types are created and their constructors compiled once per pooled builder; creating a proxy is now a delegate call.
6. Larger named-pipe server buffers, connect-path event wait instead of a spin loop, `CompressionLevel.Fastest` in the default compressor, and reused ZK cipher instances (identical ciphertext).

**Multi-targeting:** the package now ships `netstandard2.0` (unchanged 6.x dependency graph, so no new binding redirects for .NET Framework consumers) and `net8.0` (no package dependencies, plus span-based fast paths that produce identical wire bytes).

**Bug fixes:**

1. A `string[]` above the compression threshold was written with a type code no receiver could decode; it now uses `CompressedUnknown`, which every release since 1.5.0 can read.
2. Scalar `Type` parameters crashed the default serializer; they now use the wire format's `Type` code.
3. Thrown exceptions could not be serialized by System.Text.Json (`TargetSite`), which killed the connection whenever a service method threw with the default serializer; a converter now preserves the exception type, message, HResult, inner chain, and server stack trace.
4. The TCP client connected with `Socket.ConnectAsync` and waited on its completion callback, which .NET dispatches as a thread-pool work item. An application whose pool was saturated could therefore see `TimeoutException` from `new TcpClient<T>(...)` against a server that was listening the whole time, more often the fewer cores the machine had. The connect now completes on the calling thread, so `ConnectTimeOutMs` measures the network alone. Named pipes were never affected.
5. `TcpClient<T>(IPEndPoint)` now routes through `TcpEndPoint`, so its connect timeout comes from one place instead of a hard-coded literal; pass a `TcpEndPoint` to choose your own.

**Wire protocol v2** — the default on both transports, negotiated, never sent to a 6.x peer. Frame layouts and rationale: [Wire protocol](docs/wire-protocol.md).

1. Servers advertise capabilities through a new additive `ServiceSyncInfo.CapabilityFlags` member (tolerant serializers on 6.x clients ignore it; a strict custom `ISerializer` on a 6.x client may need updating — this is part of why this release is a major version).
2. Framed `MethodInvocation2`/`Response2` messages with length prefixes and correlation ids: a request payload that fails to decode is answered with a correlated error response instead of desynchronizing the stream and killing the connection.
3. `DateTime` values travel as `ToBinary()` (Kind-preserving, no string parsing).
4. Concurrent in-flight calls on a shared TCP client proxy: callers no longer serialize on a whole-round-trip lock. Whichever caller holds the read seat pairs responses to callers by correlation id while the server executes each connection's requests strictly in order; an uncontended caller reads inline with no thread handoff.
5. Named-pipe channels speak v2 with the exchange serialized per channel (no pipelining): synchronous pipe handles cannot overlap a read with a write. Pipes still gain v2's decode-error resilience and binary DateTime transfer on top of the large Tier 1 buffering wins.
6. The v2 frame costs a few microseconds per call, measurable only on loopback with strictly sequential callers. Three opt-outs force the classic v1 wire: `Host.EnableWireV2 = false` (before `AddService`) for all clients of a host, and `TcpEndPoint.UseWireV2 = false` / `NpEndPoint.UseWireV2 = false` per client.
7. In-place server downgrades with a stale cached capability produce a descriptive error and evict the cache so the next channel renegotiates.

**Opt-in additions** (defaults preserve prior behavior): TCP receive/send timeouts on `TcpEndPoint` and `TcpHost`, and a persistent log file writer via `LoggerBase.PersistentFileWriter`.

A new interop test matrix runs the published ServiceWire 6.0.1 package as a separate process against 7.0 in both directions across TCP and named pipes, with and without compression, in CI.

### Connection and Logging Reliability Fixes 6.0.1

1. Fixed retained TCP connection resources by detaching and disposing `SocketAsyncEventArgs` after connection attempts ([#90](https://github.com/tylerjensen/ServiceWire/issues/90)).
2. Added the `Host.Status` property and `HostStatus` lifecycle states so applications can detect listener failures ([#83](https://github.com/tylerjensen/ServiceWire/issues/83)).
3. Prevented the named-pipe shutdown sentinel from being processed as a client request, eliminating expected close-time errors ([#80](https://github.com/tylerjensen/ServiceWire/issues/80)).
4. Prevented named-pipe listener failures from producing an unbounded logging and CPU loop; capacity exhaustion now retries with a short backoff while other failures fault the host ([#81](https://github.com/tylerjensen/ServiceWire/issues/81)).
5. Fixed console logging so formatted messages are written instead of `System.String[]` ([#82](https://github.com/tylerjensen/ServiceWire/issues/82)).
6. Added targeted regression coverage for these fixes and repeated .NET 10 proxy creation ([#97](https://github.com/tylerjensen/ServiceWire/issues/97)).

### .NET 10 Compatibility and Performance Improvements 6.0.0

1. Fixed dynamic proxy channel validation on .NET 10 by using `Type.IsAssignableFrom`. Many thanks to [IvoTops](https://github.com/IvoTops) for contributing this fix in [pull request #98](https://github.com/tylerjensen/ServiceWire/pull/98).
2. Updated unit and integration test runs to .NET 10 while retaining .NET Framework 4.8 coverage on Windows. The ServiceWire package continues to target .NET Standard 2.0 for broad compatibility.
3. Cached method resolution, return-type conversion, and task reflection metadata to reduce repeated work during RPC calls.
4. Avoided stopwatch, statistics, debug formatting, and Base64 conversion overhead when the corresponding instrumentation is disabled.
5. Removed a redundant stream flush after the binary writer has already been flushed.
6. Reduced dictionary lookups and allocations in parameter-type mapping, service method dispatch, and pooled value storage.
7. Simplified the default GZip compression path to avoid an unnecessary input stream and decompression seek.

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

Note: Use of async/await and Task<T> not recommended. Use of Task return type not supported. While the syntax of Task return type is supported, apparently it is not marked as Serializable. In fact async/await is not really supported. Under the covers the task type is stripped away over the wire and the method is executed on a worker thread on the server synchronously. If you think about it, you will understand that it's two separate processes, so the Task Parallel Library is not going to be able to manage the thread context across the processes. RPC is inherently synchronous but the handling of each request on the host is done on thread pools. See [Async and Task returns](docs/contracts.md#async-and-task-returns) for what this means in practice.

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
