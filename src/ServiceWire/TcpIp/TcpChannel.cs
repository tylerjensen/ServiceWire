using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace ServiceWire.TcpIp
{
    public class TcpChannel : StreamingChannel
    {
        private Socket _client;

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        public TcpChannel(Type serviceType, IPEndPoint endpoint)
        {
            Initialize(serviceType, endpoint, 2500);
        }

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        public TcpChannel(Type serviceType, TcpEndPoint endpoint)
        {
            Initialize(serviceType, endpoint.EndPoint, endpoint.ConnectTimeOutMs);
        }

        private void Initialize(Type serviceType, IPEndPoint endpoint, int connectTimeoutMs)
        {
            _serviceType = serviceType;
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // TcpClient(AddressFamily.InterNetwork);
            _client.LingerState.Enabled = false;

            var connected = false;
            var connectEventArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endpoint
            };
            connectEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                connected = true;
            });

            if (_client.ConnectAsync(connectEventArgs))
            {
                //operation pending - (false means completed synchronously)
                while (!connected)
                {
                    if (!SpinWait.SpinUntil(() => connected, connectTimeoutMs))
                    {
                        if (null != _client) _client.Dispose();
                        throw new TimeoutException("Unable to connect within " + connectTimeoutMs + "ms");
                    }
                }
            }
            if (connectEventArgs.SocketError != SocketError.Success)
            {
                if (null != _client) _client.Dispose();
                throw new SocketException((int)connectEventArgs.SocketError);
            }

            //_client.Connect(endpoint);

            if (!_client.Connected) throw new SocketException(); 
            _stream = new BufferedStream(new NetworkStream(_client), 8192); //.GetStream();
            _binReader = new BinaryReader(_stream);
            _binWriter = new BinaryWriter(_stream);
            SyncInterface(_serviceType);
        }

        public override bool IsConnected { get { return (null != _client) && _client.Connected; } }

        #region IDisposable override

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _binReader.Close();
                _binWriter.Close();
                _client.Close();
                ////changed from Close to Dispose to be more complete
                //((IDisposable)_client).Dispose();
            }
        }

        #endregion
    }
}
