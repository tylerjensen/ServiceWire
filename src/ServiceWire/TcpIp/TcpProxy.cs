using System.Net;

namespace ServiceWire.TcpIp
{
    public sealed class TcpProxy
    {
        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(IPEndPoint), endpoint);
        }
    }
}
