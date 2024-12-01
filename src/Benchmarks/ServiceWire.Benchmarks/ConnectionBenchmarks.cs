using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ServiceWire.TcpIp;
using System.Net;

namespace ServiceWire.Benchmarks
{
    [MinIterationCount(4)]
    [MaxIterationCount(16)]
    [InvocationCount(64)]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class ConnectionBenchmarks
    {
        private INetTester _tester;
        private Random _rnd;

        private readonly string PipeName = "ServiceWireBenchmarkHost";

        private IPAddress _ipAddress;
        private const int Port = 8084;

        private IPEndPoint CreateTcpEndPoint(int portOffset)
        {
            return new IPEndPoint(_ipAddress, Port + portOffset);
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
            using (var tcpHost = new TcpHost(CreateTcpEndPoint(0)))
            {
                tcpHost.AddService<INetTester>(_tester);
                tcpHost.Open();
                using (var tcpClient = new TcpClient<INetTester>(CreateTcpEndPoint(0)))
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
