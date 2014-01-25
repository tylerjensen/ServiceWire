ServiceWire
===========

### A Lightweight Services Library for .NET.

ServiceWire is a very fast and light weight services host and dynamic client library that simplifies the development and use of high performance remote procedure call (RPC) communication between .NET processes over Named Pipes or TCP/IP.

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

Find "how to use" examples in the tests code. Full documentation will soon be available on the [wiki here][].

Portions of this library are a derivative of [RemotingLite][].  

  [NuGet package here]: http://www.nuget.org/packages/ServiceWire/
  [wiki here]: https://github.com/duovia/ServiceWire/wiki
  [RemotingLite]: http://remotinglite.codeplex.com/
