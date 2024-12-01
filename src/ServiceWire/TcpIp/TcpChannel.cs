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

        private Socket CreateSocket(IPEndPoint endpoint, int connectTimeoutMs)
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                LingerState = { Enabled = false }
            };

            var connected = false;
            var connectEventArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endpoint
            };
            connectEventArgs.Completed += (sender, e) => { connected = true; };

            if (client.ConnectAsync(connectEventArgs))
            {
                while (!connected)
                {
                    if (!SpinWait.SpinUntil(() => connected, connectTimeoutMs))
                    {
                        client.Dispose();
                        throw new TimeoutException($"Unable to connect within {connectTimeoutMs}ms");
                    }
                }
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

        private void Initialize(Type serviceType)
        {
            _stream = new BufferedStream(new NetworkStream(_client), 8192);
            _binReader = new BinaryReader(_stream);
            _binWriter = new BinaryWriter(_stream);

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
