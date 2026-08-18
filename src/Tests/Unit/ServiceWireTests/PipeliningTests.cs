using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
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

        public int EchoAfter(int value, int delayMs)
        {
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
    /// v2 channels pipeline: multiple threads share one proxy, each blocking only on
    /// its own correlated response while the server executes serially per connection.
    /// </summary>
    public class PipeliningTests
    {
        private static NpHost CreateHost(out string pipeName)
        {
            pipeName = "PipeliningTests" + Guid.NewGuid().ToString("N");
            var host = new NpHost(pipeName);
            host.AddService<IConcurrencyTester>(new ConcurrencyTester());
            host.Open();
            return host;
        }

        [Fact]
        public void SharedProxy_ConcurrentCalls_AreCorrectlyPaired()
        {
            using (var host = CreateHost(out var pipeName))
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

        [Fact]
        public void SharedProxy_ConcurrentIncrements_ExecuteSeriallyOnServer()
        {
            using (var host = CreateHost(out var pipeName))
            using (var client = new NpClient<IConcurrencyTester>(new NpEndPoint(pipeName)))
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
            using (var host = CreateHost(out var pipeName))
            {
                var client = new NpClient<IConcurrencyTester>(new NpEndPoint(pipeName));
                var proxy = client.Proxy;

                var pendingCall = Task.Run(() =>
                    Assert.ThrowsAny<Exception>(() => proxy.EchoAfter(1, 2000)));
                Thread.Sleep(300); //let the call get in flight
                client.Dispose();
                Assert.True(pendingCall.Wait(TimeSpan.FromSeconds(10)),
                    "pending call did not fault after client dispose");
            }
        }

    }
}
