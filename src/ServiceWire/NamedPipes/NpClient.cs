#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpClient<TInterface>:IDisposable where TInterface : class
    {
        #region Constractor

        public NpClient(NpEndPoint npAddress)
        {
            Proxy=NpProxy.CreateProxy<TInterface>(npAddress);
        }

        #endregion


        #region  Proporties

        public TInterface Proxy { get; }

        #endregion


        #region  Others

        public bool IsConnected
        {
            get { return (Proxy!=null)&&(Proxy as NpChannel).IsConnected; }
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
                    (Proxy as NpChannel).Dispose();
                }
            }
        }

        #endregion
    }
}