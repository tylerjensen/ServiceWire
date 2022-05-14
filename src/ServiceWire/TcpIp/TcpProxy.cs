using System.Net;

namespace ServiceWire.TcpIp
{
    public sealed class TcpProxy
    {
        public static TInterface CreateProxy<TInterface>(TcpZkEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs = 90000) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpZkEndPoint), endpoint, serializer, compressor, 
                identity, identityKey, log, stats, invokeTimeoutMs);
        }

        public static TInterface CreateProxy<TInterface>(TcpEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs = 90000) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(TcpEndPoint), endpoint, serializer, compressor, 
                identity, identityKey, log, stats, invokeTimeoutMs);
        }

        public static TInterface CreateProxy<TInterface>(IPEndPoint endpoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs = 90000) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(TcpChannel), typeof(IPEndPoint), endpoint, serializer, compressor, 
                identity, identityKey, log, stats, invokeTimeoutMs);
        }
    }
}
