using System;
using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpClient<TInterface> : IDisposable where TInterface : class
    {
		public TInterface Proxy { get; }

		public TcpClient(TcpEndPoint endpoint)
        {
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        public TcpClient(TcpZkEndPoint endpoint)
        {
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        public TcpClient(IPEndPoint endpoint)
        {
            Proxy = TcpProxy.CreateProxy<TInterface>(endpoint);
        }

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
