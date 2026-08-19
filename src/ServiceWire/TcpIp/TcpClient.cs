using System;
using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpClient<TInterface> : IDisposable where TInterface : class
    {
        public TInterface Proxy { get; private set; }

        public TcpClient(TcpEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null, ILog logger = null, IStats stats = null)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == logger) logger = new NullLogger();
            if (null == stats) stats = new NullStats();
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint, serializer, compressor, logger, stats);
        }

        public TcpClient(TcpZkEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null, ILog logger = null, IStats stats = null)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == logger) logger = new NullLogger();
            if (null == stats) stats = new NullStats();
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint, serializer, compressor, logger, stats);
        }

        /// <summary>
        /// Connects with the default connect timeout. Construct a <see cref="TcpEndPoint"/>
        /// instead to choose the connect timeout, or the socket receive and send timeouts.
        /// </summary>
        public TcpClient(IPEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null, ILog logger = null, IStats stats = null)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == logger) logger = new NullLogger();
            if (null == stats) stats = new NullStats();
            //through TcpEndPoint rather than the bare-IPEndPoint channel: identical
            //defaults, one less proxy type to emit, and one place that owns the timeout
            Proxy = TcpProxy.CreateProxy<TInterface>(new TcpEndPoint(endpoint), serializer, compressor, logger, stats);
        }

        [Obsolete]
        public void InjectLoggerStats(ILog logger, IStats stats)
        {
            var channel = Proxy as Channel;
            channel?.InjectLoggerStats(logger, stats);
        }

        public bool IsConnected => (Proxy as TcpChannel)?.IsConnected == true;

        #region IDisposable Members

        private bool _disposed;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    (Proxy as TcpChannel)?.Dispose();
                }
            }
        }

        #endregion
    }
}
