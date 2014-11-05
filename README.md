ServiceWire
===========

### A Lightweight Services Library for .NET.

ServiceWire is a very fast and light weight services host and dynamic client library that simplifies the development and use of high performance remote procedure call (RPC) communication between .NET processes over Named Pipes or TCP/IP.

Find "how to use" examples in the tests code. [ServiceWire documentation][] is available on the wiki.

'''Important'''

ServiceWire's dynamically generated proxy will NOT run as x86 on an x64 system. This ususally occurs when you use Visual Studio to create a console application with the default "prefer x86" in project properties selected. Just be sure to choose AnyCPU or the specific target (x86 or x64) so that you do not run 32bit in WOW on an x64 machine.

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
