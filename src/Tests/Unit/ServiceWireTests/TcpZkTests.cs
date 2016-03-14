#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWireTests
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using ServiceWire.TcpIp;
using ServiceWire.ZeroKnowledge;

#endregion


namespace ServiceWireTests
{
    public class MockRep:IZkRepository
    {
        #region Fields

        private readonly string password="cc3a6a12-0e5b-47fb-ae45-3485e34582d4";
        private readonly ZkProtocol _protocol=new ZkProtocol();
        private ZkPasswordHash _hash;

        #endregion


        #region Methods


        #region Public Methods

        public ZkPasswordHash GetPasswordHashSet(string username)
        {
            if(_hash==null)
            {
                _hash=_protocol.HashCredentials(username,password);
            }
            return _hash;
        }

        #endregion


        #endregion
    }

    [TestClass]
    public class TcpZkTests:IDisposable
    {
        #region Fields

        private Mock<INetTester> _tester;
        private readonly MockRep _repo=new MockRep();

        private readonly string username="myuser@userdomain.com";
        private readonly string password="cc3a6a12-0e5b-47fb-ae45-3485e34582d4";


        private TcpHost _tcphost;

        private IPAddress _ipAddress;

        #endregion


        #region Methods


        #region Public Methods

        [TestInitialize]
        public void RunHost()
        {
            _tester=new Mock<INetTester>();

            _tester.Setup(o => o.Min(It.IsAny<int>(),It.IsAny<int>())).Returns((int a,int b) => Math.Min(a,b));

            _tester.Setup(o => o.Range(It.IsAny<int>(),It.IsAny<int>())).Returns((int a,int b) => Enumerable.Range(a,b).ToDictionary(key => key,el => el));

            _ipAddress=IPAddress.Parse("127.0.0.1");

            _tcphost=new TcpHost(CreateEndPoint(),zkRepository:_repo);
            _tcphost.AddService(_tester.Object);
            _tcphost.Open();
        }

        [TestMethod]
        public void SimpleZkTest()
        {
            var rnd=new Random();

            var a=rnd.Next(0,100);
            var b=rnd.Next(0,100);

            var endpoint=CreateZkClientEndPoint();
            var clientProxy=new Mock<TcpClient<INetTester>>(endpoint);

            using(clientProxy.Object)
            {
                var result=clientProxy.Object.Proxy.Min(a,b);

                Assert.AreEqual(Math.Min(a,b),result,"Wrong response");
            }
        }

        [TestMethod]
        public void SimpleParallelZkTest()
        {
            var rnd=new Random();

            Parallel.For(0,50,(index,state) =>
            {
                var a=rnd.Next(0,100);
                var b=rnd.Next(0,100);

                var clientProxy=new Mock<TcpClient<INetTester>>(CreateZkClientEndPoint());

                using(clientProxy.Object)
                {
                    var result=clientProxy.Object.Proxy.Min(a,b);

                    try
                    {
                        Assert.AreEqual(Math.Min(a,b),result,"Wrong response");
                    }
                    catch(AssertFailedException)
                    {
                        state.Break();
                        throw;
                    }
                }
            });
        }

        [TestMethod]
        public void ResponseZkTest()
        {
            var clientProxy=new Mock<TcpClient<INetTester>>(CreateZkClientEndPoint());

            using(clientProxy.Object)
            {
                const int count=50;
                const int start=0;

                var result=clientProxy.Object.Proxy.Range(start,count);

                for(var i=start;i<count;i++)
                {
                    int temp;
                    if(result.TryGetValue(i,out temp))
                    {
                        Assert.AreEqual(i,temp,"Wrong value index: {0}; expected: {0}; has {1}.",i,temp);
                    } else
                    {
                        Assert.Fail("Can't find value with {0} index.",i);
                    }
                }
            }
        }

        [TestMethod]
        public void ResponseParallelTest()
        {
            Parallel.For(0,50,(index,state) =>
            {
                var clientProxy=new Mock<TcpClient<INetTester>>(CreateZkClientEndPoint());

                using(clientProxy.Object)
                {
                    const int count=50;
                    const int start=0;

                    var result=clientProxy.Object.Proxy.Range(start,count);

                    try
                    {
                        for(var i=start;i<count;i++)
                        {
                            int temp;
                            if(result.TryGetValue(i,out temp))
                            {
                                Assert.AreEqual(i,temp,"Wrong value index: {0}; expected: {0}; has {1}.",i,temp);
                            } else
                            {
                                Assert.Fail("Can't find value with {0} index.",i);
                            }
                        }
                    }
                    catch(AssertFailedException)
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
            _tcphost.Close();
        }

        #endregion


        #region Private Methods

        private IPEndPoint CreateEndPoint()
        {
            return new IPEndPoint(_ipAddress,Port);
        }

        private TcpZkEndPoint CreateZkClientEndPoint()
        {
            return new TcpZkEndPoint(username,password,new IPEndPoint(_ipAddress,Port));
        }

        #endregion


        #endregion


        #region  Others

        private const int Port=8098;

        #endregion
    }
}