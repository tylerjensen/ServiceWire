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
    [MinIterationCount(8)]
    [MaxIterationCount(64)]
    [InvocationCount(1024)]
    [SimpleJob(RuntimeMoniker.Net60, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class AllBenchmarks
    {
        private INetTester _tester;
        private NpHost _nphost;
        private NpHost _nphostJson;
        private NpClient<INetTester> _npClient = null;
        private NpClient<INetTester> _npClientJson = null;
        private Random _rnd;

        private readonly string PipeName = "ServiceWireBenchmarkHost";

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

        private NpEndPoint CreateNpEndPoint(string offset)
        {
            return new NpEndPoint(PipeName + offset);
        }

        public AllBenchmarks()
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

            _nphost = new NpHost(PipeName);
            _nphost.AddService<INetTester>(_tester);
            _nphost.Open();

            _nphostJson = new NpHost(PipeName + "Json", serializer: new NewtonsoftSerializer());
            _nphostJson.AddService<INetTester>(_tester);
            _nphostJson.Open();
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _tcpClient = new TcpClient<INetTester>(CreateTcpEndPoint(0));
            _tcpClientJson = new TcpClient<INetTester>(CreateTcpEndPoint(1));
            _npClient = new NpClient<INetTester>(CreateNpEndPoint(string.Empty));
            _npClientJson = new NpClient<INetTester>(CreateNpEndPoint("Json"), new NewtonsoftSerializer());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _tcpClient.Dispose();
            _tcpClientJson.Dispose();
            _npClient.Dispose();
            _npClientJson.Dispose();

            _tcphost.Close();
            _tcphostJson.Close();
            _nphost.Close();
            _nphostJson.Close();
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

        [Benchmark]
        public void NpSim()
        {
            var a = _rnd.Next(0, 100);
            var b = _rnd.Next(0, 100);
            var result = _npClient.Proxy.Min(a, b);
        }

        [Benchmark]
        public void NpSimJson()
        {
            var a = _rnd.Next(0, 100);
            var b = _rnd.Next(0, 100);
            var result = _npClientJson.Proxy.Min(a, b);
        }

        [Benchmark]
        public void NpRg()
        {
            var result = _npClient.Proxy.Range(0, 50);
            for (var i = 0; i < 50; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void NpRgJson()
        {
            var result = _npClientJson.Proxy.Range(0, 50);
            for (var i = 0; i < 50; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void NpCxOut()
        {
            int quantity = 0;
            var result = _npClient.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }

        [Benchmark]
        public void NpCxOutJson()
        {
            int quantity = 0;
            var result = _npClientJson.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }
    }
}
