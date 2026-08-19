using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ServiceWire.TcpIp
{
    public class TcpChannel : StreamingChannel
    {
        private readonly Socket _client;
        private readonly string _username;
        private readonly string _password;
        private readonly TcpChannelIdentifier _channelIdentifier;
        private readonly bool _allowWireV2 = true;

        /// <summary>
        /// Connect timeout used when the caller supplies a bare IPEndPoint rather than a
        /// TcpEndPoint. Use TcpEndPoint to choose your own.
        /// </summary>
        internal const int DefaultConnectTimeOutMs = 2500;

        protected override bool AllowWireV2 => _allowWireV2;

        public TcpChannel(Type serviceType, IPEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _username = null;
            _password = null;
            _channelIdentifier = new TcpChannelIdentifier(endpoint);
            _client = CreateSocket(endpoint, DefaultConnectTimeOutMs);
            Initialize(serviceType);
        }

        public TcpChannel(Type serviceType, TcpEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _username = null;
            _password = null;
            _channelIdentifier = new TcpChannelIdentifier(endpoint.EndPoint);
            _client = CreateSocket(endpoint.EndPoint, endpoint.ConnectTimeOutMs);
            if (endpoint.ReceiveTimeoutMs > 0) _client.ReceiveTimeout = endpoint.ReceiveTimeoutMs;
            if (endpoint.SendTimeoutMs > 0) _client.SendTimeout = endpoint.SendTimeoutMs;
            _allowWireV2 = endpoint.UseWireV2;
            Initialize(serviceType);
        }

        public TcpChannel(Type serviceType, TcpZkEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (endpoint.Username == null) throw new ArgumentNullException(nameof(endpoint.Username));
            if (endpoint.Password == null) throw new ArgumentNullException(nameof(endpoint.Password));

            _username = endpoint.Username;
            _password = endpoint.Password;
            _channelIdentifier = new TcpChannelIdentifier(endpoint.EndPoint);
            _client = CreateSocket(endpoint.EndPoint, endpoint.ConnectTimeOutMs);
            Initialize(serviceType);
        }

        /// <summary>
        /// Opens a connected socket, giving up after <paramref name="connectTimeoutMs"/>.
        /// </summary>
        /// <remarks>
        /// The connect completes on the calling thread. The obvious alternative - ConnectAsync
        /// with a SocketAsyncEventArgs and a wait on its Completed callback - makes the
        /// observed connect time depend on thread-pool health, because .NET dispatches that
        /// callback as a pool work item. With the pool saturated, a loopback connect that
        /// finishes in well under a millisecond is not observed for hundreds of milliseconds,
        /// and the caller sees a TimeoutException against a server that was listening the
        /// whole time. A non-blocking connect followed by Socket.Select never leaves this
        /// thread, so the timeout measures the network and nothing else.
        /// </remarks>
        internal static Socket CreateSocket(IPEndPoint endpoint, int connectTimeoutMs)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = { Enabled = false },
                NoDelay = true //request/response traffic is written through a BufferedStream and flushed once per message, so Nagle only adds latency
            };

            try
            {
                client.Blocking = false;
                try
                {
                    client.Connect(endpoint);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock)
                {
                    //expected: the handshake is in flight and is awaited below
                }

                //a loopback connect can finish inside Connect, leaving nothing to wait for
                if (!client.Connected) AwaitConnect(client, connectTimeoutMs);

                //the rest of the library does blocking I/O over this socket
                client.Blocking = true;
                return client;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        private static void AwaitConnect(Socket client, int connectTimeoutMs)
        {
            //Select counts microseconds in an int, so clamp rather than overflow into a
            //negative value, which Select reads as "wait forever"
            const int maxMillisecondsBeforeOverflow = int.MaxValue / 1000;
            var microseconds = connectTimeoutMs <= 0
                ? 0
                : (connectTimeoutMs > maxMillisecondsBeforeOverflow ? int.MaxValue : connectTimeoutMs * 1000);

            //Select rather than two Poll calls: a connect that succeeds lands in the write
            //set, but Windows reports a refused connect only in the error set. Polling the
            //write set first would therefore wait out the whole timeout before ever looking
            //at the error set, turning a fast refusal into a slow one. Select waits on both
            //at once, so whichever the platform signals ends the wait immediately.
            var writeCheck = new List<Socket> { client };
            var errorCheck = new List<Socket> { client };
            Socket.Select(null, writeCheck, errorCheck, microseconds);

            if (writeCheck.Count == 0 && errorCheck.Count == 0)
                throw new TimeoutException($"Unable to connect within {connectTimeoutMs}ms");

            var socketError = (SocketError)(int)client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
            if (socketError != SocketError.Success) throw new SocketException((int)socketError);

            if (!client.Connected) throw new SocketException((int)SocketError.NotConnected);
        }

        private void Initialize(Type serviceType)
        {
            //independent read and write buffers over one NetworkStream: v2 channels
            //read responses on a dedicated thread while callers write, and a shared
            //BufferedStream cannot serve concurrent readers and writers (its single
            //buffer requires a seek to switch modes). NetworkStream itself supports
            //one concurrent reader plus one concurrent writer.
            _stream = new NetworkStream(_client);
            _binReader = new BinaryReader(new BufferedStream(_stream, 8192));
            _binWriter = new BinaryWriter(new BufferedStream(_stream, 8192));

            try
            {
                SyncInterface(serviceType, _username, _password);
            }
            catch
            {
                Dispose(true);
                throw;
            }
        }

        protected override IChannelIdentifier ChannelIdentifier => _channelIdentifier;

        public override bool IsConnected => _client?.Connected ?? false;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _client?.Dispose();
            }
        }
    }
}
