using System;
using ServiceWire.NamedPipes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ServiceWire.TcpIp;
using System.Net;

namespace ServiceWire.Benchmarks
{
    //explicit, modest invocation counts: every operation opens a real TCP
    //connection whose close lingers in TIME_WAIT for minutes, so an unbounded run
    //exhausts the Windows ephemeral port pool and fails with address-in-use
    [SimpleJob(RuntimeMoniker.Net80, baseline: true, warmupCount: 2, iterationCount: 15, invocationCount: 16)]
    [SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 2, iterationCount: 15, invocationCount: 16)]
    [SimpleJob(RuntimeMoniker.Net48, warmupCount: 2, iterationCount: 15, invocationCount: 16)]
    [MemoryDiagnoser]
    [HtmlExporter]
    public class ConnectionBenchmarks
    {
        private INetTester _tester;
        private Random _rnd;

        //distinct pipe name: sharing it with the steady-state benchmark classes
        //lets a stray host instance in another process serve these clients
        private readonly string PipeName = "ServiceWireBenchmarkConn";

        private IPAddress _ipAddress;

        //a genuinely free port per operation: this benchmark opens and closes a host
        //every iteration, and connections closed by the previous iteration's server
        //linger in TIME_WAIT for minutes, which blocks rebinding their port on
        //Windows. The OS-assigned probe port never collides; its ~10 microsecond
        //cost is part of every measured connection setup equally.
        private static IPEndPoint GetFreeEndPoint()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var endPoint = (IPEndPoint)probe.LocalEndpoint;
            probe.Stop();
            return endPoint;
        }

        private NpEndPoint CreateNpEndPoint(string offset)
        {
            return new NpEndPoint(PipeName + offset);
        }

        public ConnectionBenchmarks()
        {
            _rnd = new Random();
            _tester = new NetTester();
            _ipAddress = IPAddress.Parse("127.0.0.1");
        }

        [Benchmark]
        public void TcpConn()
        {
            var endPoint = GetFreeEndPoint();
            using (var tcpHost = new TcpHost(endPoint))
            {
                tcpHost.AddService<INetTester>(_tester);
                tcpHost.Open();
                using (var tcpClient = new TcpClient<INetTester>(endPoint))
                {
                    var a = _rnd.Next(0, 100);
                    var b = _rnd.Next(0, 100);
                    var result = tcpClient.Proxy.Min(a, b);
                }
            }
        }

        [Benchmark]
        public void NpConn()
        {
            using (var npHost = new NpHost(PipeName))
            {
                npHost.AddService<INetTester>(_tester);
                npHost.Open();
                using (var npClient = new NpClient<INetTester>(CreateNpEndPoint(string.Empty)))
                {
                    var a = _rnd.Next(0, 100);
                    var b = _rnd.Next(0, 100);
                    var result = npClient.Proxy.Min(a, b);
                }
            }
        }
    }
}
