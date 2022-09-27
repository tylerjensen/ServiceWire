using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using Xunit;

namespace ServiceWireTests
{
    public class NpTests : IDisposable
    {
        private INetTester _tester;

        private NpHost _nphost;

        private readonly string _pipeName = "ServiceWireTestHost";

        private NpEndPoint CreateEndPoint()
        {
            return new NpEndPoint(_pipeName);
        }

        public NpTests()
        {
            var rnd = new Random(DateTime.UtcNow.Millisecond);
            var val = rnd.Next(999, 99999);
            _pipeName += val.ToString();

            _tester = new NetTester();
            _nphost = new NpHost(_pipeName);
            _nphost.AddService<INetTester>(_tester);
            _nphost.Open();
        }

        [Fact]
        public void SimpleTest()
        {
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
            {
                var result = clientProxy.Proxy.Min(a, b);
                Assert.Equal(Math.Min(a, b), result);
            }
        }

        [Fact]
        public void SimpleNewtonsoftSerializerTest()
        {
            using (var nphost = new NpHost(_pipeName + "Json", serializer: new NewtonsoftSerializer()))
            {
                nphost.AddService<INetTester>(_tester);
                nphost.Open();

                var rnd = new Random();

                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                using (var clientProxy = new NpClient<INetTester>(new NpEndPoint(_pipeName + "Json"), new NewtonsoftSerializer()))
                {
                    var result = clientProxy.Proxy.Min(a, b);
                    Assert.Equal(Math.Min(a, b), result);
                }
            }
        }

        [Fact]
        public void SimpleProtobufSerializerTest()
        {
            using (var nphost = new NpHost(_pipeName + "Proto", serializer: new ProtobufSerializer()))
            {
                nphost.AddService<INetTester>(_tester);
                nphost.Open();

                var rnd = new Random();

                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                using (var clientProxy = new NpClient<INetTester>(new NpEndPoint(_pipeName + "Proto"), new ProtobufSerializer()))
                {
                    var result = clientProxy.Proxy.Min(a, b);
                    Assert.Equal(Math.Min(a, b), result);
                }
            }
        }

        [Fact]
        public async Task CalculateAsyncTest()
        {
	        var rnd = new Random();

	        var a = rnd.Next(0, 100);
	        var b = rnd.Next(0, 100);

	        using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
	        {
		        var result = await clientProxy.Proxy.CalculateAsync(a, b);
		        Assert.Equal(a + b, result);
	        }
        }

		[Fact]
        public void SimpleParallelTest()
        {
            var rnd = new Random();

            Parallel.For(0, 4, (index, state) =>
            {
                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
                {
                    var result = clientProxy.Proxy.Min(a, b);

                    if (Math.Min(a, b) != result) state.Break();
                    Assert.Equal(Math.Min(a, b), result);
                }
            });
        }

        [Fact]
        public void ResponseTest()
        {
            using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
            {
                const int count = 50;
                const int start = 0;

                var result = clientProxy.Proxy.Range(start, count);

                for (var i = start; i < count; i++)
                {
                    int temp;
                    Assert.True(result.TryGetValue(i, out temp));
                    Assert.Equal(i, temp);
                }
            }
        }

        [Fact]
        public void ResponseParallelTest()
        {
            Parallel.For(0, 4, (index, state) =>
            {
                using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
                {
                    const int count = 5;
                    const int start = 0;

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
            });
        }

        [Fact]
        public void ResponseWithOutParameterTest()
        {
            using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
            {
                int quantity = 0;
                var result = clientProxy.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
                Assert.Equal(44, quantity);
                Assert.NotEqual(default(TestResponse), result);
                Assert.Equal("MyLabel", result.Label);
            }
        }

        [Fact]
        public void ResponseWithOutParameterNewtonsoftSerializerTest()
        {
            using (var nphost = new NpHost(_pipeName + "JsonResponseOut", serializer: new NewtonsoftSerializer()))
            {
                nphost.AddService<INetTester>(_tester);
                nphost.Open();

                using (var clientProxy = new NpClient<INetTester>(new NpEndPoint(_pipeName + "JsonResponseOut"), new NewtonsoftSerializer()))
                {
                    int quantity = 0;
                    var result = clientProxy.Proxy.Get(Guid.NewGuid(), "SomeLabel", 45.65, out quantity);
                    Assert.Equal(44, quantity);
                    Assert.NotEqual(default(TestResponse), result);
                    Assert.Equal("MyLabel", result.Label);
                }
            }
        }

        [Fact]
        public void GetStringsTest()
        {
            using (var clientProxy = new NpClient<INetTester>(CreateEndPoint()))
            {
                var result = clientProxy.Proxy.GetStrings();
                Assert.Equal(4, result.Length);
                Assert.Null(result[2]);
            }
        }

        public void Dispose()
        {
            _nphost.Close();
        }
    }
}
