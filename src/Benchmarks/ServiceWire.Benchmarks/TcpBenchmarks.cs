using System;
using ServiceWire.TcpIp;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Net;

namespace ServiceWire.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net10_0)]
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
        private IPEndPoint _endPoint;
        private IPEndPoint _endPointJson;

        //fixed ports fail intermittently with address-in-use: each benchmark child
        //process rebinds the same port, and connections closed by the previous
        //child's server linger in TIME_WAIT, which blocks the next bind on Windows
        private static IPEndPoint GetFreeEndPoint()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var endPoint = (IPEndPoint)probe.LocalEndpoint;
            probe.Stop();
            return endPoint;
        }

        public TcpBenchmarks()
        {
            _rnd = new Random();
            _tester = new NetTester();
            _ipAddress = IPAddress.Parse("127.0.0.1");
        }

        //hosts are created in GlobalSetup, not the constructor: BenchmarkDotNet also
        //instantiates this class in its orchestrating host process for validation,
        //and a host opened there holds the fixed ports so the measured child process
        //cannot bind them (every TCP benchmark then fails with address-in-use)
        [GlobalSetup]
        public void GlobalSetup()
        {
            _endPoint = GetFreeEndPoint();
            _endPointJson = GetFreeEndPoint();

            _tcphost = new TcpHost(_endPoint);
            _tcphost.AddService<INetTester>(_tester);
            _tcphost.Open();

            _tcphostJson = new TcpHost(_endPointJson);
            _tcphostJson.AddService<INetTester>(_tester);
            _tcphostJson.Open();

            _tcpClient = new TcpClient<INetTester>(_endPoint);
            _tcpClientJson = new TcpClient<INetTester>(_endPointJson);
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
