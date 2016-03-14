#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System.Net;

#endregion


namespace ServiceWire.TcpIp
{
    public sealed class TcpProxy
    {
        #region Methods


        #region Public Methods

        public static TInterface CreateProxy<TInterface>(TcpZkEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel),typeof(TcpZkEndPoint),endpoint);
        }

        public static TInterface CreateProxy<TInterface>(TcpEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel),typeof(TcpEndPoint),endpoint);
        }

        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel),typeof(IPEndPoint),endpoint);
        }

        #endregion


        #endregion
    }
}