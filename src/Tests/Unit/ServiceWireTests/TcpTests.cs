using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using ServiceWire.TcpIp;

using Moq;
using NUnit.Framework;

namespace ServiceWireTests
{
    [TestFixture]
    public class TcpTests : IDisposable
    {
        private Mock<INetTester> _tester;

        private TcpHost _tcphost;

        private IPAddress _ipAddress;

        private const int Port = 8098;

        private IPEndPoint CreateEndPoint()
        {
            return new IPEndPoint(_ipAddress, Port);
        }

        [TestFixtureSetUp]
        public void RunHost()
        {
            _tester = new Mock<INetTester>();

            _tester
                .Setup(o => o.Min(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int a, int b) => Math.Min(a, b));

            _tester
                .Setup(o => o.Range(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int a, int b) => Enumerable.Range(a, b).ToDictionary(key => key, el => el));

            _ipAddress = IPAddress.Parse("127.0.0.1");

            _tcphost = new TcpHost(CreateEndPoint());
            _tcphost.AddService<INetTester>(_tester.Object);
            _tcphost.Open();
        }

        [Test]
        public void SimpleTest()
        {
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            var clientProxy = new Mock<TcpClient<INetTester>>(CreateEndPoint());

            using (clientProxy.Object)
            {
                var result = clientProxy.Object.Proxy.Min(a, b);

                Assert.AreEqual(Math.Min(a, b), result, "Wrong response");
            }
        }

        [Test]
        public void SimpleParallelTest()
        {
            var rnd = new Random();

            Parallel.For(0, 50, (index, state) =>
            {
                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                var clientProxy = new Mock<TcpClient<INetTester>>(CreateEndPoint());

                using (clientProxy.Object)
                {
                    var result = clientProxy.Object.Proxy.Min(a, b);

                    try
                    {
                        Assert.AreEqual(Math.Min(a, b), result, "Wrong response");
                    }
                    catch (AssertionException)
                    {
                        state.Break();
                        throw;
                    }
                }
            });
        }

        [Test]
        public void ResponseTest()
        {
            var clientProxy = new Mock<TcpClient<INetTester>>(CreateEndPoint());

            using (clientProxy.Object)
            {
                const int count = 50;
                const int start = 0;

                var result = clientProxy.Object.Proxy.Range(start, count);

                for (var i = start; i < count; i++)
                {
                    int temp;
                    if (result.TryGetValue(i, out temp))
                    {
                        Assert.AreEqual(i, temp, "Wrong value index: {0}; expected: {0}; has {1}.", i, temp);
                    }
                    else
                    {
                        Assert.Fail("Can't find value with {0} index.", i);
                    }
                }
            }
        }

        [Test]
        public void ResponseParallelTest()
        {
            Parallel.For(0, 50, (index, state) =>
            {
                var clientProxy = new Mock<TcpClient<INetTester>>(CreateEndPoint());

                using (clientProxy.Object)
                {
                    const int count = 50;
                    const int start = 0;

                    var result = clientProxy.Object.Proxy.Range(start, count);

                    try
                    {
                        for (var i = start; i < count; i++)
                        {
                            int temp;
                            if (result.TryGetValue(i, out temp))
                            {
                                Assert.AreEqual(i, temp, "Wrong value index: {0}; expected: {0}; has {1}.", i, temp);
                            }
                            else
                            {
                                Assert.Fail("Can't find value with {0} index.", i);
                            }
                        }
                    }
                    catch (AssertionException)
                    {
                        state.Break();
                        throw;
                    }
                }
            });
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            _tcphost.Close();
        }
    }
}
