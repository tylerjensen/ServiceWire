using System;

namespace ServiceWire
{
    public abstract class Channel : IDisposable
    {
        protected Type _serviceType;

        protected ILog _logger;
        protected IStats _stats;
        internal readonly ISerializer _serializer;
        internal readonly ICompressor _compressor;

        public Channel(ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
        {
            _serializer = serializer ?? new DefaultSerializer();
            _compressor = compressor ?? new DefaultCompressor();
            _logger = logger ?? new NullLogger();
            _stats = stats ?? new NullStats();
        }

        [Obsolete]
        public void InjectLoggerStats(ILog logger, IStats stats)
        {
            _logger = logger;
            _stats = stats;
        }

        /// <summary>
        /// Invokes the method with the specified parameters.
        /// </summary>
        /// <param name="parameters">Parameters for the method call</param>
        /// <returns>An array of objects containing the return value (index 0) and the parameters used to call
        /// the method, including any marked as "ref" or "out"</returns>
        protected abstract object[] InvokeMethod(string metaData, params object[] parameters);

        /// <summary>
        /// Channel must implement an interface synchronization method.
        /// This method asks the server for a list of identifiers paired with method
        /// names and -parameter types. This is used when invoking methods server side.
        /// When username and password supplied, zero knowledge encryption is used.
        /// </summary>
        protected abstract void SyncInterface(Type serviceType,
            string username = null, string password = null);

        #region IDisposable Members

        protected bool _disposed = false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}
