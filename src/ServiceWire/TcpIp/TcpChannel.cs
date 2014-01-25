using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
            _serviceType = serviceType;
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // TcpClient(AddressFamily.InterNetwork);
            _client.LingerState.Enabled = false;
            _client.Connect(endpoint);
            if (!_client.Connected) throw new SocketException(); 
            _stream = new BufferedStream(new NetworkStream(_client), 8192); //.GetStream();
            _binReader = new BinaryReader(_stream);
            _binWriter = new BinaryWriter(_stream);
            _formatter = new BinaryFormatter();
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
