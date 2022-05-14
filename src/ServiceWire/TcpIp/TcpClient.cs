using System;
using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpClient<TInterface> : IDisposable where TInterface : class
    {
        public TInterface Proxy { get; }

        public TcpClient(TcpEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null,
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == log) log = new NullLogger();
            if (null == stats) stats = new NullStats();
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint, serializer, compressor, identity, identityKey, log, stats, invokeTimeoutMs);
        }

        public TcpClient(TcpZkEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null,
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == log) log = new NullLogger();
            if (null == stats) stats = new NullStats();
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint, serializer, compressor, identity, identityKey, log, stats, invokeTimeoutMs);
        }

        public TcpClient(IPEndPoint endpoint, ISerializer serializer = null, ICompressor compressor = null,
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == log) log = new NullLogger();
            if (null == stats) stats = new NullStats();
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint, serializer, compressor, identity, identityKey, log, stats, invokeTimeoutMs);
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
