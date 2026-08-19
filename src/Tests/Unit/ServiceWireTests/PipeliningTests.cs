using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using Xunit;

namespace ServiceWireTests
{
    public interface IConcurrencyTester
    {
        int EchoAfter(int value, int delayMs);
        int Increment();
    }

    public class ConcurrencyTester : IConcurrencyTester
    {
        //deliberately not thread safe: the server contract is serial execution
        //per connection, which this counter verifies (no lost updates)
        private int _count;

        /// <summary>
        /// Signalled as soon as a delayed call starts executing on the host, so a test
        /// can know a call is genuinely in flight instead of guessing with a sleep.
        /// </summary>
        public readonly ManualResetEventSlim CallStarted = new ManualResetEventSlim(false);

        public int EchoAfter(int value, int delayMs)
        {
            CallStarted.Set();
            if (delayMs > 0) Thread.Sleep(delayMs);
            return value;
        }

        public int Increment()
        {
            var next = _count + 1;
            Thread.Sleep(1); //widen the race window if execution were concurrent
            _count = next;
            return next;
        }
    }

    /// <summary>
    /// v2 TCP channels pipeline: multiple threads share one proxy, each blocking only
    /// on its own correlated response while the server executes serially per
    /// connection. Named-pipe channels serialize the exchange (the transport cannot
    /// overlap reads and writes) but must still pair results correctly.
    /// </summary>
    public class PipeliningTests
    {
        private static TcpHost CreateTcpHost(out int port)
        {
            ConcurrencyTester ignored;
            return CreateTcpHost(out port, out ignored);
        }

        private static TcpHost CreateTcpHost(out int port, out ConcurrencyTester service)
        {
            port = TestPorts.GetFreePort();
            service = new ConcurrencyTester();
            var host = new TcpHost(port);
            host.AddService<IConcurrencyTester>(service);
            host.Open();
            return host;
        }

        private static TcpClient<IConcurrencyTester> CreateTcpClient(int port)
        {
            return new TcpClient<IConcurrencyTester>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), 5000));
        }

        [Fact]
        public void SharedTcpProxy_ConcurrentCalls_AreCorrectlyPaired()
        {
            using (var host = CreateTcpHost(out var port))
            using (var client = CreateTcpClient(port))
            {
                var proxy = client.Proxy;
                var tasks = Enumerable.Range(0, 24).Select(i =>
                    Task.Run(() => new { Sent = i, Received = proxy.EchoAfter(i, 0) })).ToArray();
                Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
                foreach (var t in tasks)
                    Assert.Equal(t.Result.Sent, t.Result.Received);
            }
        }

        [Fact]
        public void SharedNpProxy_ConcurrentCalls_AreCorrectlyPaired()
        {
            var pipeName = "PipeliningTests" + Guid.NewGuid().ToString("N");
            using (var host = new NpHost(pipeName))
            {
                host.AddService<IConcurrencyTester>(new ConcurrencyTester());
                host.Open();
                using (var client = new NpClient<IConcurrencyTester>(new NpEndPoint(pipeName)))
                {
                    var proxy = client.Proxy;
                    var tasks = Enumerable.Range(0, 24).Select(i =>
                        Task.Run(() => new { Sent = i, Received = proxy.EchoAfter(i, 0) })).ToArray();
                    Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
                    foreach (var t in tasks)
                        Assert.Equal(t.Result.Sent, t.Result.Received);
                }
            }
        }

        [Fact]
        public void SharedTcpProxy_ConcurrentIncrements_ExecuteSeriallyOnServer()
        {
            using (var host = CreateTcpHost(out var port))
            using (var client = CreateTcpClient(port))
            {
                var proxy = client.Proxy;
                const int callers = 16;
                var results = new List<int>();
                var tasks = Enumerable.Range(0, callers).Select(_ =>
                    Task.Run(() => proxy.Increment())).ToArray();
                Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
                foreach (var t in tasks) results.Add(t.Result);

                //serial execution: no lost updates and every intermediate value distinct
                Assert.Equal(callers, results.Distinct().Count());
                Assert.Equal(callers, results.Max());
            }
        }

        [Fact]
        public void DisposeWithPendingCall_FaultsThePendingCaller()
        {
            using (var host = CreateTcpHost(out var port, out var service))
            {
                var client = CreateTcpClient(port);
                var proxy = client.Proxy;

                var pendingCall = Task.Run(() =>
                    Assert.ThrowsAny<Exception>(() => proxy.EchoAfter(1, 2000)));

                //wait for the host to actually start executing the call rather than
                //sleeping and hoping: disposing before it is in flight tests nothing
                Assert.True(service.CallStarted.Wait(TimeSpan.FromSeconds(10)),
                    "the call never reached the host");
                client.Dispose();
                Assert.True(pendingCall.Wait(TimeSpan.FromSeconds(10)),
                    "pending call did not fault after client dispose");
            }
        }
    }
}
