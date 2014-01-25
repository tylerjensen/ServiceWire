using System;
using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpClient<TInterface> : IDisposable where TInterface : class
    {
        private TInterface _proxy;

        public TInterface Proxy { get { return _proxy; } }

        public TcpClient(IPEndPoint endpoint)
        {
            _proxy = TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        public bool IsConnected
        {
            get
            {
                return (_proxy != null) && (_proxy as TcpChannel).IsConnected;
            }
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
                    (_proxy as TcpChannel).Dispose();
                }
            }
        }

        #endregion
    }
}
