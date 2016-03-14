#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion


namespace ServiceWire.TcpIp
{
    public class TcpChannel:StreamingChannel
    {
        #region Constractor

        /// <summary>
        ///     Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        public TcpChannel(Type serviceType,IPEndPoint endpoint)
        {
            Initialize(null,null,serviceType,endpoint,2500);
        }

        /// <summary>
        ///     Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        public TcpChannel(Type serviceType,TcpEndPoint endpoint)
        {
            Initialize(null,null,serviceType,endpoint.EndPoint,endpoint.ConnectTimeOutMs);
        }

        public TcpChannel(Type serviceType,TcpZkEndPoint endpoint)
        {
            if(endpoint==null)
            {
                throw new ArgumentNullException("endpoint");
            }
            if(endpoint.Username==null)
            {
                throw new ArgumentNullException("endpoint.Username");
            }
            if(endpoint.Password==null)
            {
                throw new ArgumentNullException("endpoint.Password");
            }
            Initialize(endpoint.Username,endpoint.Password,serviceType,endpoint.EndPoint,endpoint.ConnectTimeOutMs);
        }

        #endregion


        #region Fields

        private Socket _client;
        private string _username;
        private string _password;

        #endregion


        #region Methods


        #region Private Methods

        private void Initialize(string username,string password,Type serviceType,IPEndPoint endpoint,int connectTimeoutMs)
        {
            _username=username;
            _password=password;
            _serviceType=serviceType;
            _client=new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp); // TcpClient(AddressFamily.InterNetwork);
            _client.LingerState.Enabled=false;

            var connected=false;
            var connectEventArgs=new SocketAsyncEventArgs {RemoteEndPoint=endpoint};
            connectEventArgs.Completed+=(sender,e) => { connected=true; };

            if(_client.ConnectAsync(connectEventArgs))
            {
                //operation pending - (false means completed synchronously)
                while(!connected)
                {
                    if(!SpinWait.SpinUntil(() => connected,connectTimeoutMs))
                    {
#if (!NET35)
                        _client.Dispose();
#else
                        _client.Close();
#endif
                        throw new TimeoutException("Unable to connect within "+connectTimeoutMs+"ms");
                    }
                }
            }
            if(connectEventArgs.SocketError!=SocketError.Success)
            {
#if (!NET35)
                _client.Dispose();
#else
                _client.Close();
#endif
                throw new SocketException((int)connectEventArgs.SocketError);
            }
            if(!_client.Connected)
            {
#if (!NET35)
                _client.Dispose();
#else
                _client.Close();
#endif
                throw new SocketException((int)SocketError.NotConnected);
            }
            _stream=new BufferedStream(new NetworkStream(_client),8192);
            _binReader=new BinaryReader(_stream);
            _binWriter=new BinaryWriter(_stream);
            try
            {
                SyncInterface(_serviceType,_username,_password);
            }
            catch(Exception e)
            {
                Dispose(true);
                throw;
            }
        }

        #endregion


        #region Protected Methods


        #region IDisposable override

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing)
            {
#if (!NET35)
                _client.Dispose();
#else
                _client.Close();
#endif
            }
        }

        #endregion


        #endregion


        #endregion


        #region  Others

        public override bool IsConnected
        {
            get { return (null!=_client)&&_client.Connected; }
        }

        #endregion
    }
}