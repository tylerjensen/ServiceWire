using ServiceWire.ZeroKnowledge;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWire.TcpIp
{
    public class TcpHost : Host
    {
        private Socket _listener;
        private IPEndPoint _endPoint;
        private ManualResetEvent _listenResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// Constructs an instance of the host and starts listening for incoming connections on any ip address.
        /// All listener threads are regular background threads.
        /// </summary>
        /// <param name="port">The port number for incoming requests</param>
        /// <param name="log"></param>
        /// <param name="stats"></param>
        /// <param name="zkRepository">Only required to support zero knowledge authentication and encryption.</param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public TcpHost(int port, ILog log = null, IStats stats = null,
            IZkRepository zkRepository = null, ISerializer serializer = null, ICompressor compressor = null)
            : base(serializer, compressor)
        {
            Initialize(new IPEndPoint(IPAddress.Any, port), log, stats, zkRepository);
        }

        /// <summary>
        /// Constructs an instance of the host and starts listening for incoming connections on designated endpoint.
        /// All listener threads are regular background threads.
        /// 
        /// NOTE: the instance created from the specified type is not automatically thread safe!
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="log"></param>
        /// <param name="stats"></param>
        /// <param name="zkRepository">Only required to support zero knowledge authentication and encryption.</param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        public TcpHost(IPEndPoint endpoint, ILog log = null, IStats stats = null,
            IZkRepository zkRepository = null, ISerializer serializer = null, ICompressor compressor = null)
            : base(serializer, compressor)
        {
            Initialize(endpoint, log, stats, zkRepository);
        }

        private void Initialize(IPEndPoint endpoint, ILog log, IStats stats, IZkRepository zkRepository)
        {
            base.Log = log;
            base.Stats = stats;
            base.ZkRepository = zkRepository ?? new ZkNullRepository();
            _endPoint = endpoint;
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#if NET462
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
#endif
        }

        /// <summary>
        /// Gets the end point this host is listening on
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        protected override void StartListener()
        {
            Task.Factory.StartNew(Listen, TaskCreationOptions.LongRunning);
        }

        private SocketAsyncEventArgs _acceptEventArg;

        /// <summary>
        /// Listens for incoming tcp requests.
        /// </summary>
        private void Listen()
        {
            try
            {
                _listener.Bind(_endPoint);
                _listener.Listen(8192);

                _acceptEventArg = new SocketAsyncEventArgs();
                _acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(acceptEventArg_Completed);

                while (!_disposed)
                {
                    // Set the event to nonsignaled state.
                    _listenResetEvent.Reset();
                    _acceptEventArg.AcceptSocket = null;
                    try
                    {
                        if (!_listener.AcceptAsync(_acceptEventArg))
                        {
                            AcceptNewClient(_acceptEventArg);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Listen error: {0}", ex.ToString().Flatten());
                        break; //break loop on unhandled
                    }

                    // Wait until a connection is made before continuing.
                    _listenResetEvent.WaitOne();
                }
            }
            catch (Exception e)
            {
                _log.Fatal("Listen fatal error: {0}", e.ToString().Flatten());
            }
        }

        private void acceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            AcceptNewClient(e);
        }

        private void AcceptNewClient(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError != SocketError.Success)
                {
                    if (!_disposed) _listenResetEvent.Set();
                    return;
                }

                Socket activeSocket = null;
                BufferedStream stream = null;
                try
                {
                    activeSocket = e.AcceptSocket;

                    // Signal the listening thread to continue.
                    _listenResetEvent.Set();

                    stream = new BufferedStream(new NetworkStream(activeSocket), 8192);
                    base.ProcessRequest(stream);
                }
                catch (Exception ex)
                {
                    _log.Error("AcceptNewClient_ProcessRequest error: {0}", ex.ToString().Flatten());
                }
                finally
                {
                    if (null != stream)
                    {
                        stream.Close();
                    }
                    if (null != activeSocket && activeSocket.Connected)
                    {
                        try
                        {
                            activeSocket.Shutdown(SocketShutdown.Both);
                        }
                        catch (Exception shutdownException)
                        {
                            _log.Error("AcceptNewClient_ActiveSocketShutdown error: {0}", shutdownException.ToString().Flatten());
                        }

                        try
                        {
                            activeSocket.Close();
                        }
                        catch (Exception closeException)
                        {
                            _log.Error("AcceptNewClient_ActiveSocketClose error: {0}", closeException.ToString().Flatten());
                        }
                    }
                }
            }
            catch (Exception fatalException)
            {
                _log.Fatal("AcceptNewClient fatal error: {0}", fatalException.ToString().Flatten());
            }
        }

        #region IDisposable Members

        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    _listenResetEvent.Set();
                    _acceptEventArg.Dispose();
                    _listener.Close();
                    _listenResetEvent.Close();
                }
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
