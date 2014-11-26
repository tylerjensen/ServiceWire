using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceWire.NamedPipes;
using Moq;

namespace ServiceWireTests
{
    [TestClass]
    public class NpTests : IDisposable
    {
        private Mock<INetTester> _tester;

        private NpHost _nphost;

        private const string PipeName = "ServiceWireTestHost";

        private static NpEndPoint CreateEndPoint()
        {
            return new NpEndPoint(PipeName);
        }

        [TestInitialize]
        public void RunHost()
        {
            _tester = new Mock<INetTester>();

            _tester
                .Setup(o => o.Min(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int a, int b) => Math.Min(a, b));

            _tester
                .Setup(o => o.Range(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int a, int b) => Enumerable.Range(a, b).ToDictionary(key => key, el => el));

            _nphost = new NpHost(PipeName);
            _nphost.AddService<INetTester>(_tester.Object);
            _nphost.Open();
        }

        [TestMethod]
        public void SimpleTest()
        {
            var rnd = new Random();

            var a = rnd.Next(0, 100);
            var b = rnd.Next(0, 100);

            var clientProxy = new Mock<NpClient<INetTester>>(CreateEndPoint());

            using (clientProxy.Object)
            {
                var result = clientProxy.Object.Proxy.Min(a, b);

                Assert.AreEqual(Math.Min(a, b), result, "Wrong response");
            }
        }

        [TestMethod]
        public void SimpleParallelTest()
        {
            var rnd = new Random();

            Parallel.For(0, 50, (index, state) =>
            {
                var a = rnd.Next(0, 100);
                var b = rnd.Next(0, 100);

                var clientProxy = new Mock<NpClient<INetTester>>(CreateEndPoint());

                using (clientProxy.Object)
                {
                    var result = clientProxy.Object.Proxy.Min(a, b);

                    try
                    {
                        Assert.AreEqual(Math.Min(a, b), result, "Wrong response");
                    }
                    catch (AssertFailedException)
                    {
                        state.Break();
                        throw;
                    }
                }
            });
        }

        [TestMethod]
        public void ResponseTest()
        {
            var clientProxy = new Mock<NpClient<INetTester>>(CreateEndPoint());

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

        [TestMethod]
        public void ResponseParallelTest()
        {
            Parallel.For(0, 50, (index, state) =>
            {
                var clientProxy = new Mock<NpClient<INetTester>>(CreateEndPoint());

                using (clientProxy.Object)
                {
                    const int count = 5;
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
                    catch (AssertFailedException)
                    {
                        state.Break();
                        throw;
                    }
                }
            });
        }

        [TestCleanup]
        public void Dispose()
        {
            _nphost.Close();
        }
    }
}
