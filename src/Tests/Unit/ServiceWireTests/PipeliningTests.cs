using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
            return new TcpClient<IConcurrencyTester>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port), TestPorts.ConnectTimeoutMs));
        }

        /// <summary>
        /// Runs <paramref name="count"/> callers concurrently on dedicated threads and
        /// returns their results in order.
        /// <para>
        /// Dedicated threads rather than Task.Run: every caller blocks for a whole round
        /// trip, and this suite runs xUnit collections in parallel. Blocking dozens of
        /// thread-pool threads starves the pool for the rest of the run, which delays the
        /// socket completion callbacks other tests' clients wait on and makes them time
        /// out against hosts that are listening perfectly well.
        /// </para>
        /// </summary>
        private static T[] RunConcurrently<T>(int count, Func<int, T> caller)
        {
            var results = new T[count];
            var failures = new Exception[count];
            var threads = new Thread[count];
            for (var i = 0; i < count; i++)
            {
                var index = i;
                threads[i] = new Thread(() =>
                {
                    //capture rather than throw: an escaping exception on a raw thread
                    //takes the whole test host down
                    try { results[index] = caller(index); }
                    catch (Exception e) { failures[index] = e; }
                })
                { IsBackground = true };
                threads[i].Start();
            }

            foreach (var thread in threads)
                Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "a concurrent caller did not finish");

            foreach (var failure in failures)
                if (null != failure) throw failure;

            return results;
        }

        [Fact]
        public void SharedTcpProxy_ConcurrentCalls_AreCorrectlyPaired()
        {
            using (var host = CreateTcpHost(out var port))
            using (var client = CreateTcpClient(port))
            {
                var proxy = client.Proxy;
                var received = RunConcurrently(24, i => proxy.EchoAfter(i, 0));
                for (var i = 0; i < received.Length; i++)
                    Assert.Equal(i, received[i]);
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
                    var received = RunConcurrently(24, i => proxy.EchoAfter(i, 0));
                    for (var i = 0; i < received.Length; i++)
                        Assert.Equal(i, received[i]);
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
                var results = RunConcurrently(callers, _ => proxy.Increment());

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

                //a dedicated thread, for the same reason as RunConcurrently
                Exception thrown = null;
                var pendingCall = new Thread(() =>
                {
                    try { proxy.EchoAfter(1, 2000); }
                    catch (Exception e) { thrown = e; }
                })
                { IsBackground = true };
                pendingCall.Start();

                //wait for the host to actually start executing the call rather than
                //sleeping and hoping: disposing before it is in flight tests nothing
                Assert.True(service.CallStarted.Wait(TimeSpan.FromSeconds(10)),
                    "the call never reached the host");
                client.Dispose();
                Assert.True(pendingCall.Join(TimeSpan.FromSeconds(10)),
                    "pending call did not fault after client dispose");
                Assert.NotNull(thrown);
            }
        }
    }
}
