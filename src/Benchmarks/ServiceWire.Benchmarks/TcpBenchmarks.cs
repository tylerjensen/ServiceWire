using System;
using ServiceWire.TcpIp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Net;

namespace ServiceWire.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    [HtmlExporter]
    public class TcpBenchmarks
    {
        private INetTester _tester;
        private Random _rnd;

        private TcpHost _tcphost;
        private TcpHost _tcphostJson;

        private TcpClient<INetTester> _tcpClient;
        private TcpClient<INetTester> _tcpClientJson;

        private IPAddress _ipAddress;
        private const int Port = 8084;
        private IPEndPoint CreateTcpEndPoint(int portOffset)
        {
            return new IPEndPoint(_ipAddress, Port + portOffset);
        }

        public TcpBenchmarks()
        {
            _rnd = new Random();
            _tester = new NetTester();
            _ipAddress = IPAddress.Parse("127.0.0.1");
            _tcphost = new TcpHost(CreateTcpEndPoint(0));
            _tcphost.AddService<INetTester>(_tester);
            _tcphost.Open();

            _tcphostJson = new TcpHost(CreateTcpEndPoint(1));
            _tcphostJson.AddService<INetTester>(_tester);
            _tcphostJson.Open();
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _tcpClient = new TcpClient<INetTester>(CreateTcpEndPoint(0));
            _tcpClientJson = new TcpClient<INetTester>(CreateTcpEndPoint(1));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _tcpClient.Dispose();
            _tcpClientJson.Dispose();
            _tcphost.Close();
            _tcphostJson.Close();
        }

        [Benchmark]
        public void TcpSim()
        {
            var a = _rnd.Next(0, 100);
            var b = _rnd.Next(0, 100);
            var result = _tcpClient.Proxy.Min(a, b);
        }

        [Benchmark]
        public void TcpSimJson()
        {
            var a = _rnd.Next(0, 100);
            var b = _rnd.Next(0, 100);
            var result = _tcpClientJson.Proxy.Min(a, b);
        }

        [Benchmark]
        public void TcpRg()
        {
            var result = _tcpClient.Proxy.Range(0, 50);
            for (var i = 0; i < 50; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void TcpRgJson()
        {
            var result = _tcpClientJson.Proxy.Range(0, 50);
            for (var i = 0; i < 50; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void TcpCxOut()
        {
            int quantity = 0;
            var result = _tcpClient.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }

        [Benchmark]
        public void TcpCxOutJson()
        {
            int quantity = 0;
            var result = _tcpClientJson.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }
    }
}
