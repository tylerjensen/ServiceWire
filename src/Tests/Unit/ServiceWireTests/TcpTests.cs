using System;
using System.Net;
using System.Threading.Tasks;
using ServiceWire.TcpIp;
using Xunit;

namespace ServiceWireTests
{
    [Collection("Sequential Collection Tcp")]
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
            Task.Delay(100);
            _clientProxy = new TcpClient<INetTester>(CreateEndPoint());
        }

        [Fact]
        public void SimpleTest()
        {
            Task.Delay(100);
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            var result = _clientProxy.Proxy.Min(a, b);
            Assert.Equal(Math.Min(a, b), result);
            Task.Delay(100);
        }

        [Fact]
        public async Task CalculateAsyncTest()
        {
            await Task.Delay(100);
            var rnd = new Random();

	        var a = rnd.Next(0, 100);
	        var b = rnd.Next(0, 100);

		    var result = await _clientProxy.Proxy.CalculateAsync(a, b);
		    Assert.Equal(a + b, result);
            await Task.Delay(100);
        }

		[Fact]
        public void SimpleParallelTest()
        {
            Task.Delay(100);
            var rnd = new Random();

            Parallel.For(0, 4, (index, state) =>
            {
                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                using (var clientProxy = new TcpClient<INetTester>(CreateEndPoint()))
                {
                    var result = clientProxy.Proxy.Min(a, b);
                    if (Math.Min(a, b) != result)
                    {
                        state.Break();
                        Assert.Equal(Math.Min(a, b), result);
                    }
                }
                Task.Delay(100);
            });
            Task.Delay(100);
        }

        [Fact]
        public void ResponseTest()
        {
            Task.Delay(100);
            const int count = 50;
            const int start = 0;

            var result = _clientProxy.Proxy.Range(start, count);

            for (var i = start; i < count; i++)
            {
                int temp;
                Assert.True(result.TryGetValue(i, out temp));
                Assert.Equal(i, temp);
            }
            Task.Delay(100);
        }

        [Fact]
        public void ResponseParallelTest()
        {
            Task.Delay(100);
            Parallel.For(0, 4, (index, state) =>
            {
                const int count = 50;
                const int start = 0;

                using (var clientProxy = new TcpClient<INetTester>(CreateEndPoint()))
                {
                    var result = clientProxy.Proxy.Range(start, count);
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
                }
                Task.Delay(100);
            });
            Task.Delay(100);
        }

        [Fact]
        public void ResponseWithOutParameterTest()
        {
            Task.Delay(100);
            int quantity = 0;
            var result = _clientProxy.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
            Assert.Equal(44, quantity);
            Assert.NotEqual(default(TestResponse), result);
            Assert.Equal("MyLabel", result.Label);
            Task.Delay(100);
        }

        [Fact]
        public void GetStringsTest()
        {
            Task.Delay(100);
            var result = _clientProxy.Proxy.GetStrings();
            Assert.Equal(4, result.Length);
            Assert.Null(result[2]);
            Task.Delay(100);
        }


        public void Dispose()
        {
            _clientProxy.Dispose();
            _tcphost.Close();
            _tcphost.Dispose();
        }
    }
}
