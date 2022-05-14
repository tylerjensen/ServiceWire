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
        /// <param name="sessionTimeoutMins">How long in minutes before the host will close an unused client connection. Default is 20 minutes.</param>
        public TcpHost(int port, ILog log = null, IStats stats = null,
            IZkRepository zkRepository = null, ISerializer serializer = null, ICompressor compressor = null, int sessionTimeoutMins = 20)
            : base("tcp://localhost:" + port, sessionTimeoutMins, serializer, compressor, log, stats, zkRepository)
        {
            _endPoint = new IPEndPoint(IPAddress.Any, port);
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
        /// <param name="sessionTimeoutMins">How long in minutes before the host will close an unused client connection. Default is 20 minutes.</param>
        public TcpHost(IPEndPoint endpoint, ILog log = null, IStats stats = null,
            IZkRepository zkRepository = null, ISerializer serializer = null, ICompressor compressor = null, int sessionTimeoutMins = 20)
            : base("tcp://" + endpoint.Address.ToString() + ":" + endpoint.Port, sessionTimeoutMins, serializer, compressor, log, stats, zkRepository)
        {
            _endPoint = endpoint;
        }

        /// <summary>
        /// Gets the end point this host is listening on
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
        }
    }
}
