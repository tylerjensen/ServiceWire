using System;
using System.Net;
using System.Threading.Tasks;
using ServiceWire.TcpIp;
using ServiceWire.ZeroKnowledge;
using Xunit;

namespace ServiceWireTests
{
    public class FakeZkRepository : IZkRepository
    {
        private string password = "cc3a6a12-0e5b-47fb-ae45-3485e34582d4";
        private ZkProtocol _protocol = new ZkProtocol();
        private ZkPasswordHash _hash = null;

        public ZkPasswordHash GetPasswordHashSet(string username)
        {
            if (_hash == null) _hash = _protocol.HashCredentials(username, password);
            return _hash;
        }
    }

    public class TcpZkTests : IDisposable
    {
        private INetTester _tester;
        private FakeZkRepository _repo = new FakeZkRepository();

        private string username = "myuser@userdomain.com";
        private string password = "cc3a6a12-0e5b-47fb-ae45-3485e34582d4";

        private TcpHost _tcphost;
        private IPAddress _ipAddress;
        private const int Port = 8098;
        private TcpClient<INetTester> _clientProxy;

        private IPEndPoint CreateEndPoint()
        {
            return new IPEndPoint(_ipAddress, Port);
        }

        private TcpZkEndPoint CreateZkClientEndPoint()
        {
            return new TcpZkEndPoint(username, password, new IPEndPoint(_ipAddress, Port), connectTimeOutMs: 5000); //expand timeout for CI/CD pipeline
        }

        public TcpZkTests()
        {
            _tester = new NetTester();
            _ipAddress = IPAddress.Parse("127.0.0.1");

            _tcphost = new TcpHost(CreateEndPoint(), zkRepository: _repo);
            _tcphost.AddService<INetTester>(_tester);
            _tcphost.Open();

            _clientProxy = new TcpClient<INetTester>(CreateZkClientEndPoint());
        }

        [Fact]
        public void SimpleZkTest()
        {
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            var result = _clientProxy.Proxy.Min(a, b);
            Assert.Equal<int>(Math.Min(a, b), result);
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
        public void SimpleParallelZkTest()
        {
            var rnd = new Random();
            Parallel.For(0, 12, (index, state) =>
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
        public void ResponseZkTest()
        {
            const int count = 50;
            const int start = 0;

            var result = _clientProxy.Proxy.Range(start, count);

            for (var i = start; i < count; i++)
            {
                int temp;
                if (result.TryGetValue(i, out temp))
                {
                    Assert.Equal(i, temp);
                }
                else
                {
                    Assert.True(false);
                }
            }
        }

        [Fact]
        public void ResponseParallelTest()
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            Parallel.For(0, 12, (index, state) =>
            {
                    const int count = 50;
                    const int start = 0;

                    var result = _clientProxy.Proxy.Range(start, count);

                    for (var i = start; i < count; i++)
                    {
                        int temp;
                        if (result.TryGetValue(i, out temp))
                        {
                            if(i != temp) state.Break();
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

        public void Dispose()
        {
            _clientProxy.Dispose();
            _tcphost.Close();
            _tcphost.Dispose();
        }
    }
}
