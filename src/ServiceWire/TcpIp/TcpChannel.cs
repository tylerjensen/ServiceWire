using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServiceWire.TcpIp
{
    public class TcpChannel : StreamingChannel
    {
        private readonly Socket _client;
        private readonly string _username;
        private readonly string _password;
        private readonly TcpChannelIdentifier _channelIdentifier;
        private readonly bool _allowWireV2 = true;

        protected override bool AllowWireV2 => _allowWireV2;

        public TcpChannel(Type serviceType, IPEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _username = null;
            _password = null;
            _channelIdentifier = new TcpChannelIdentifier(endpoint);
            _client = CreateSocket(endpoint, 2500);
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

        internal static Socket CreateSocket(IPEndPoint endpoint, int connectTimeoutMs,
            Func<SocketAsyncEventArgs> connectEventArgsFactory = null,
            Action<SocketAsyncEventArgs> connectEventArgsDisposer = null)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = { Enabled = false },
                NoDelay = true //request/response traffic is written through a BufferedStream and flushed once per message, so Nagle only adds latency
            };

            var connectedEvent = new ManualResetEventSlim(false);
            SocketAsyncEventArgs connectEventArgs = null;
            EventHandler<SocketAsyncEventArgs> completedHandler = (sender, e) =>
            {
                try
                {
                    connectedEvent.Set();
                }
                catch (ObjectDisposedException)
                {
                    //a timed-out connect can complete after cleanup; nothing to signal
                }
            };

            try
            {
                connectEventArgs = connectEventArgsFactory?.Invoke() ?? new SocketAsyncEventArgs();
                connectEventArgs.RemoteEndPoint = endpoint;
                connectEventArgs.Completed += completedHandler;

                //ConnectAsync returning false means it completed synchronously, so
                //there is nothing to wait for
                if (client.ConnectAsync(connectEventArgs) && !connectedEvent.Wait(connectTimeoutMs))
                {
                    client.Dispose();
                    throw new TimeoutException($"Unable to connect within {connectTimeoutMs}ms");
                }
                if (connectEventArgs.SocketError != SocketError.Success)
                {
                    client.Dispose();
                    throw new SocketException((int)connectEventArgs.SocketError);
                }
                if (!client.Connected)
                {
                    client.Dispose();
                    throw new SocketException((int)SocketError.NotConnected);
                }

                return client;
            }
            catch
            {
                client.Dispose();
                throw;
            }
            finally
            {
                if (connectEventArgs != null)
                {
                    connectEventArgs.Completed -= completedHandler;
                    connectEventArgs.AcceptSocket = null;
                    connectEventArgs.RemoteEndPoint = null;
                    if (connectEventArgsDisposer == null)
                        connectEventArgs.Dispose();
                    else
                        connectEventArgsDisposer(connectEventArgs);
                }
                connectedEvent.Dispose();
            }
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
