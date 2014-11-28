using System.Net;

namespace ServiceWire.TcpIp
{
    public sealed class TcpProxy
    {
        public static TInterface CreateProxy<TInterface>(TcpZkEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpZkEndPoint), endpoint);
        }

        public static TInterface CreateProxy<TInterface>(TcpEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpEndPoint), endpoint);
        }

        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(IPEndPoint), endpoint);
        }
    }
}
