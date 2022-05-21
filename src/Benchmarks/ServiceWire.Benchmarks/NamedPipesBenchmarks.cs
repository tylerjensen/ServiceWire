using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ServiceWire.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60, baseline: true)]
    [SimpleJob(RuntimeMoniker.Net48)]
    [MemoryDiagnoser]
    public class NamedPipesBenchmarks : IDisposable
    {
        private INetTester _tester;
        private NpHost _nphost;
        private NpHost _nphostJson;
        private NpHost _nphostProto;

        private readonly string PipeName = "ServiceWireBenchmarkHost";

        private NpEndPoint CreateEndPoint()
        {
            return new NpEndPoint(PipeName);
        }

        public NamedPipesBenchmarks()
        {
            //var rnd = new Random();
            //var rndNum = rnd.Next(0, 10000000);
            //PipeName += rndNum.ToString();

            _tester = new NetTester();
            _nphost = new NpHost(PipeName);
            _nphost.AddService<INetTester>(_tester);
            _nphost.Open();

            _nphostJson = new NpHost(PipeName + "Json", serializer: new NewtonsoftSerializer());
            _nphostJson.AddService<INetTester>(_tester);
            _nphostJson.Open();

            _nphostProto = new NpHost(PipeName + "Proto", serializer: new ProtobufSerializer());
            _nphostProto.AddService<INetTester>(_tester);
            _nphostProto.Open();
        }

        NpClient<INetTester> clientProxy = null;
        NpClient<INetTester> clientProxyJson = null;
        NpClient<INetTester> clientProxyProto = null;

        [GlobalSetup]
        public void GlobalSetup()
        {
            clientProxy = new NpClient<INetTester>(CreateEndPoint());
            clientProxyJson = new NpClient<INetTester>(new NpEndPoint(PipeName + "Json"), new NewtonsoftSerializer());
            clientProxyProto = new NpClient<INetTester>(new NpEndPoint(PipeName + "Proto"), new ProtobufSerializer());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            clientProxy.Dispose();
            clientProxyJson.Dispose();
            clientProxyProto.Dispose();
        }

        [Benchmark]
        public void NpSim()
        {
            var rnd = new Random();
            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);
            var result = clientProxy.Proxy.Min(a, b);
        }

        [Benchmark]
        public void NpSimJson()
        {
            var rnd = new Random();
            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);
            var result = clientProxyJson.Proxy.Min(a, b);
        }

        [Benchmark]
        public void NpRg()
        {
            const int count = 50;
            const int start = 0;
            var result = clientProxy.Proxy.Range(start, count);
            for (var i = start; i < count; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void NpRgJson()
        {
            const int count = 50;
            const int start = 0;
            var result = clientProxyJson.Proxy.Range(start, count);
            for (var i = start; i < count; i++)
            {
                int temp;
                result.TryGetValue(i, out temp);
            }
        }

        [Benchmark]
        public void NpCxOut()
        {
            int quantity = 0;
            var result = clientProxy.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }

        [Benchmark]
        public void NpCxOutJson()
        {
            int quantity = 0;
            var result = clientProxyJson.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
        }

        public void Dispose()
        {
            _nphost.Close();
            _nphostJson.Close();
            _nphostProto.Close();
        }
    }
}
