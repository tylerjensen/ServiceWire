using System;
using System.Net;
using System.Threading.Tasks;
using ServiceWire.TcpIp;
using Xunit;

namespace ServiceWireTests
{
    public class TcpTests : IDisposable
    {
        private INetTester _tester;
        private TcpHost _tcphost;
        private IPAddress _ipAddress;
        private const int Port = 8099;
        private TcpClient<INetTester> _clientProxy;

        private IPEndPoint CreateEndPoint()
        {
            return new IPEndPoint(_ipAddress, Port);
        }

        public TcpTests()
        {
            _tester = new NetTester();

            _ipAddress = IPAddress.Parse("127.0.0.1");

            _tcphost = new TcpHost(CreateEndPoint());
            _tcphost.AddService<INetTester>(_tester);
            _tcphost.Open();

            _clientProxy = new TcpClient<INetTester>(CreateEndPoint());
        }

        [Fact]
        public void SimpleTest()
        {
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            var result = _clientProxy.Proxy.Min(a, b);
            Assert.Equal(Math.Min(a, b), result);
        }

        [Fact]
        public async Task CalculateAsyncTest()
        {
	        var rnd = new Random();

	        var a = rnd.Next(0, 100);
	        var b = rnd.Next(0, 100);

		    var result = await _clientProxy.Proxy.CalculateAsync(a, b);
		    Assert.Equal(a + b, result);
        }

		[Fact]
        public void SimpleParallelTest()
        {
            var rnd = new Random();

            Parallel.For(0, 4, (index, state) =>
            {
                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                var result = _clientProxy.Proxy.Min(a, b);

                if (Math.Min(a, b) != result)
                {
                    state.Break();
                    Assert.Equal(Math.Min(a, b), result);
                }
            });
        }

        [Fact]
        public void ResponseTest()
        {
            const int count = 50;
            const int start = 0;

            var result = _clientProxy.Proxy.Range(start, count);

            for (var i = start; i < count; i++)
            {
                int temp;
                Assert.True(result.TryGetValue(i, out temp));
                Assert.Equal(i, temp);
            }
        }

        [Fact]
        public void ResponseParallelTest()
        {
            Parallel.For(0, 4, (index, state) =>
            {
                const int count = 50;
                const int start = 0;

                var result = _clientProxy.Proxy.Range(start, count);
                for (var i = start; i < count; i++)
                {
                    int temp;
                    if (result.TryGetValue(i, out temp))
                    {
                        if (i != temp) state.Break();
                        Assert.Equal(i, temp);
                    }
                    else
                    {
                        state.Break();
                        Assert.True(false);
                    }
                }
            });
        }

        [Fact]
        public void ResponseWithOutParameterTest()
        {
            int quantity = 0;
            var result = _clientProxy.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
            Assert.Equal(44, quantity);
            Assert.NotEqual(default(TestResponse), result);
            Assert.Equal("MyLabel", result.Label);
        }

        [Fact]
        public void GetStringsTest()
        {
            var result = _clientProxy.Proxy.GetStrings();
            Assert.Equal(4, result.Length);
            Assert.Null(result[2]);
        }


        public void Dispose()
        {
            _clientProxy.Dispose();
            _tcphost.Close();
            _tcphost.Dispose();
        }
    }
}
