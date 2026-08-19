using ServiceWire;
using ServiceWire.NamedPipes;
using ServiceWire.TcpIp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ServiceWireTests
{
    [Collection("Issue regressions")]
    public class IssueRegressionTests
    {
        [Fact]
        public void Issue80_ClosingNamedPipeHostDoesNotProcessSentinelConnection()
        {
            var logger = new RecordingLogger();
            var host = new NpHost("ServiceWire-Issue80-" + Guid.NewGuid(), logger);

            host.Open();
            host.Close();

            Assert.False(logger.WaitForError(250));
            Assert.Empty(logger.Errors);
        }

        [Fact]
        public void Issue81_NonCapacityNamedPipeFailureStopsInsteadOfSpinning()
        {
            var logger = new RecordingLogger();
            var listener = new NpListener(
                "ServiceWire-Issue81-" + Guid.NewGuid(),
                log: logger,
                streamFactory: new ThrowingNamedPipeServerStreamFactory(
                    new UnauthorizedAccessException("Access denied for regression test.")));

            listener.Start();

            Assert.True(logger.WaitForFatal(2000));
            Thread.Sleep(100);
            Assert.Single(logger.FatalMessages);
            Assert.Contains("UnauthorizedAccessException", logger.FatalMessages.Single());
            Assert.DoesNotContain(logger.Errors,
                message => message.Contains("ProcessNextClient error"));

            listener.Stop();
        }

        [Fact]
        public void Issue82_ConsoleLoggerWritesFormattedLines()
        {
            var originalOut = Console.Out;
            var output = new StringWriter();
            Console.SetOut(output);

            try
            {
                var logger = new Logger(
                    logDirectory: Path.GetTempPath(),
                    logLevel: LogLevel.Info,
                    messageBufferSize: 4096,
                    options: LogOptions.LogOnlyToConsole);

                logger.Info("Processing request - Name:{0}, Id: {1}", "alpha", 42);
                logger.FlushLog();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Contains("Processing request - Name:alpha, Id: 42", output.ToString());
            Assert.DoesNotContain("System.String[]", output.ToString());
        }

        [Fact]
        public void Issue83_TcpHostExposesOpenClosedAndFaultedStatus()
        {
            var endpoint = GetUnusedLoopbackEndpoint();
            var host = new TcpHost(endpoint);

            Assert.Equal(HostStatus.Created, host.Status);
            host.Open();
            Assert.Equal(HostStatus.Open, host.Status);
            host.Close();
            Assert.Equal(HostStatus.Closed, host.Status);

            var blocker = new TcpListener(IPAddress.Loopback, 0);
            blocker.Start();
            try
            {
                var blockedEndpoint = (IPEndPoint)blocker.LocalEndpoint;
                var faultedHost = new TcpHost(blockedEndpoint);

                Assert.ThrowsAny<SocketException>(() => faultedHost.Open());
                Assert.Equal(HostStatus.Faulted, faultedHost.Status);
                faultedHost.Close();
                Assert.Equal(HostStatus.Closed, faultedHost.Status);
            }
            finally
            {
                blocker.Stop();
            }
        }

        /// <summary>
        /// Issue #90 was a SocketAsyncEventArgs retained after every connection attempt.
        /// The connect path no longer uses SocketAsyncEventArgs at all - it connects on the
        /// calling thread - so that specific leak is prevented by construction. What still
        /// needs pinning is the behaviour the fix was really about: a connect attempt must
        /// not retain resources, whether it succeeds or fails.
        /// </summary>
        [Fact]
        public async Task Issue90_TcpConnectRetainsNothingOnSuccessOrFailure()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var acceptTask = listener.AcceptSocketAsync();
                using (var client = TcpChannel.CreateSocket((IPEndPoint)listener.LocalEndpoint, 2500))
                using (var server = await acceptTask)
                {
                    //the rest of the channel depends on both of these
                    Assert.True(client.Connected);
                    Assert.True(client.Blocking);
                    using (var stream = new NetworkStream(client))
                    {
                        Assert.True(stream.CanRead);
                    }
                }

                //a failed connect must dispose the socket it created rather than leak it.
                //Kept to a few iterations on purpose: a refused loopback connect costs about
                //two seconds on Windows (SYN retransmit) no matter how the wait is done.
                var closed = ClosedLoopbackEndPoint();
                for (var i = 0; i < 3; i++)
                    Assert.ThrowsAny<SocketException>(() => TcpChannel.CreateSocket(closed, 2500));

                var acceptAgain = listener.AcceptSocketAsync();
                using (var client = TcpChannel.CreateSocket((IPEndPoint)listener.LocalEndpoint, 2500))
                using (var server = await acceptAgain)
                {
                    Assert.True(client.Connected);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// A connect must not depend on thread-pool scheduling. The previous implementation
        /// waited on a SocketAsyncEventArgs.Completed callback, which .NET dispatches as a
        /// pool work item: with the pool saturated, a loopback connect that finishes in under
        /// a millisecond went unobserved for hundreds of milliseconds and the caller saw a
        /// TimeoutException against a listening server.
        /// </summary>
        [Fact]
        public void TcpConnect_SucceedsWhileThreadPoolIsSaturated()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            ThreadPool.GetMinThreads(out var minWorker, out var minIo);
            var release = new ManualResetEventSlim(false);
            try
            {
                //a small pool plus more blocking work items than it has threads: any
                //pool-dispatched continuation now queues behind them
                ThreadPool.SetMinThreads(2, minIo);
                for (var i = 0; i < 64; i++)
                    ThreadPool.QueueUserWorkItem(_ => release.Wait(TimeSpan.FromSeconds(30)));
                Thread.Sleep(200); //let the blockers take the available threads

                //Measured under exactly these conditions: the old pool-dispatched wait took
                //512-1017 ms, this path takes 0-8 ms. 250 ms sits well clear of both, and
                //only elapses while Poll is actually waiting on the handshake, so an
                //unrelated scheduling hiccup cannot consume the budget.
                using (var client = TcpChannel.CreateSocket((IPEndPoint)listener.LocalEndpoint, 250))
                {
                    Assert.True(client.Connected);
                }
            }
            finally
            {
                release.Set();
                ThreadPool.SetMinThreads(minWorker, minIo);
                listener.Stop();
            }
        }

        private static IPEndPoint ClosedLoopbackEndPoint()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var endPoint = (IPEndPoint)probe.LocalEndpoint;
            probe.Stop();
            return endPoint;
        }

        [Fact]
        public void Issue97_RepeatedProxyCreationAcceptsChannelSubclass()
        {
            for (var i = 0; i < 100; i++)
            {
                var proxy = ProxyFactory.CreateProxy<IIssue97Contract>(
                    typeof(Issue97Channel),
                    typeof(string),
                    "channel",
                    new DefaultSerializer(),
                    new DefaultCompressor(),
                    new RecordingLogger(),
                    null);

                Assert.NotNull(proxy);
                ((IDisposable)proxy).Dispose();
            }
        }

        private static IPEndPoint GetUnusedLoopbackEndpoint()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            listener.Stop();
            return endpoint;
        }

        private sealed class ThrowingNamedPipeServerStreamFactory : INamedPipeServerStreamFactory
        {
            private readonly Exception _exception;

            public ThrowingNamedPipeServerStreamFactory(Exception exception)
            {
                _exception = exception;
            }

            public NamedPipeServerStream Create(string pipeName, PipeDirection direction,
                int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode,
                PipeOptions options, int inBufferSize, int outBufferSize)
            {
                throw _exception;
            }
        }

        private sealed class RecordingLogger : ILog
        {
            private readonly ManualResetEventSlim _error = new ManualResetEventSlim();
            private readonly ManualResetEventSlim _fatal = new ManualResetEventSlim();

            public ConcurrentQueue<string> Errors { get; } = new ConcurrentQueue<string>();
            public ConcurrentQueue<string> FatalMessages { get; } = new ConcurrentQueue<string>();

            public bool WaitForError(int milliseconds) => _error.Wait(milliseconds);
            public bool WaitForFatal(int milliseconds) => _fatal.Wait(milliseconds);

            public void Debug(string formattedMessage, params object[] args) { }
            public void Info(string formattedMessage, params object[] args) { }
            public void Warn(string formattedMessage, params object[] args) { }

            public void Error(string formattedMessage, params object[] args)
            {
                Errors.Enqueue(Format(formattedMessage, args));
                _error.Set();
            }

            public void Fatal(string formattedMessage, params object[] args)
            {
                FatalMessages.Enqueue(Format(formattedMessage, args));
                _fatal.Set();
            }

            private static string Format(string formattedMessage, object[] args)
            {
                return args == null || args.Length == 0
                    ? formattedMessage
                    : string.Format(formattedMessage, args);
            }
        }

        public interface IIssue97Contract
        {
            int Ping();
        }

        public class Issue97Channel : Channel
        {
            public Issue97Channel(Type serviceType, string channel, ISerializer serializer,
                ICompressor compressor, ILog logger, IStats stats)
                : base(serializer, compressor, logger, stats)
            {
                _serviceType = serviceType;
            }

            protected override object[] InvokeMethod(string metaData, params object[] parameters)
            {
                return new object[] { 1 };
            }

            protected override void SyncInterface(Type serviceType,
                string username = null, string password = null)
            {
            }

            protected override void Dispose(bool disposing)
            {
                _disposed = true;
            }
        }
    }
}
