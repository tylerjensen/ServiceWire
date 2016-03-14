#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Net;

#endregion


namespace ServiceWire.TcpIp
{
    public class TcpClient<TInterface>:IDisposable where TInterface : class
    {
        #region Constractor

        public TcpClient(TcpEndPoint endpoint)
        {
            Proxy=TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        public TcpClient(TcpZkEndPoint endpoint)
        {
            Proxy=TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        public TcpClient(IPEndPoint endpoint)
        {
            Proxy=TcpProxy.CreateProxy<TInterface>(endpoint);
        }

        #endregion


        #region  Proporties

        public TInterface Proxy { get; }

        #endregion


        #region  Others

        public bool IsConnected
        {
            get { return (Proxy!=null)&&(Proxy as TcpChannel).IsConnected; }
        }

        #endregion


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
            if(!_disposed)
            {
                _disposed=true; //prevent second call to Dispose
                if(disposing)
                {
                    (Proxy as TcpChannel).Dispose();
                }
            }
        }

        #endregion
    }
}