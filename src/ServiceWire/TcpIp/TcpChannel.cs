using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServiceWire.TcpIp
{
    public class TcpChannel : StreamingChannel
    {
        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        public TcpChannel(Type serviceType, IPEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs)
            : base(serializer, compressor, "tcp://" + endpoint.Address.ToString() + ":" + endpoint.Port, identity, identityKey, log, stats, invokeTimeoutMs)
        {
            Initialize(serviceType);
        }

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        public TcpChannel(Type serviceType, TcpEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs)
            : base(serializer, compressor, "tcp://" + endpoint.EndPoint.Address.ToString() + ":" + endpoint.EndPoint.Port, identity, identityKey, log, stats, invokeTimeoutMs)
        {
            Initialize(serviceType);
        }

        /// <summary>
        /// Creates a secure connection to the concrete object handling method calls on the server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="endpoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        public TcpChannel(Type serviceType, TcpZkEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs)
            : base(serializer, compressor, "tcp://" + endpoint.EndPoint.Address.ToString() + ":" + endpoint.EndPoint.Port,
                  identity, identityKey, log, stats, invokeTimeoutMs)
        {
            if (endpoint == null) throw new ArgumentNullException("endpoint");
            if (endpoint.Username == null) throw new ArgumentNullException("endpoint.Username");
            if (endpoint.Password == null) throw new ArgumentNullException("endpoint.Password");
            Initialize(serviceType);
        }

        private void Initialize(Type serviceType)
        {
            _serviceType = serviceType;
            try
            {
                SyncInterface();
            }
            catch
            {
                this.Dispose(true);
                throw;
            }
        }

        #region IDisposable override

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }
}
