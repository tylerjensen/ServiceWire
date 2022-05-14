using System;

namespace ServiceWire.NamedPipes
{
    public class NpClient<TInterface> : IDisposable where TInterface : class
    {
        private TInterface _proxy;

        public TInterface Proxy { get { return _proxy; } }

        public bool IsConnected
        {
            get
            {
                return (_proxy != null) && (_proxy as NpChannel).IsConnected;
            }
        }

        /// <summary>
        /// Create a named pipes client.
        /// </summary>
        /// <param name="npAddress"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        public NpClient(NpEndPoint npAddress, ISerializer serializer = null, ICompressor compressor = null,
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000)
        {
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == log) log = new NullLogger();
            if (null == stats) stats = new NullStats();
            _proxy = NpProxy.CreateProxy<TInterface>(npAddress, serializer, compressor, identity, identityKey, log, stats, invokeTimeoutMs);
        }

        #region IDisposable Members

        private bool _disposed = false;

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
                    (_proxy as NpChannel).Dispose();
                }
            }
        }

        #endregion
    }
}
