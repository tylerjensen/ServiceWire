using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ServiceWire.Benchmarks
{
    [MinIterationCount(8)]
    [MaxIterationCount(64)]
    [InvocationCount(1024)]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class NamedPipesBenchmarks
    {
        private INetTester _tester;
        private NpHost _nphost;
        private NpHost _nphostJson;
        private NpClient<INetTester> _npClient = null;
        private NpClient<INetTester> _npClientJson = null;
        private Random _rnd;

        private readonly string PipeName = "ServiceWireBenchmarkHost";

        private NpEndPoint CreateNpEndPoint(string offset)
        {
            return new NpEndPoint(PipeName + offset);
        }

        public NamedPipesBenchmarks()
        {
            _rnd = new Random();

            _tester = new NetTester();
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
            _npClient = new NpClient<INetTester>(CreateNpEndPoint(string.Empty));
            _npClientJson = new NpClient<INetTester>(CreateNpEndPoint("Json"), new NewtonsoftSerializer());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _npClient.Dispose();
            _npClientJson.Dispose();
            _nphost.Close();
            _nphostJson.Close();
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
