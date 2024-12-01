using System.Net;

namespace ServiceWire.TcpIp
{
    public static class TcpProxy
    {
        public static TInterface CreateProxy<TInterface>(TcpZkEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpZkEndPoint), endpoint, serializer, compressor, logger, stats);
        }

        public static TInterface CreateProxy<TInterface>(TcpEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpEndPoint), endpoint, serializer, compressor, logger, stats);
        }

        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(IPEndPoint), endpoint, serializer, compressor, logger, stats);
        }
    }
}
