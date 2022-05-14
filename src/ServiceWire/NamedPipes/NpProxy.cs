namespace ServiceWire.NamedPipes
{
    public class NpProxy
    {
        public static TInterface CreateProxy<TInterface>(NpEndPoint npAddress, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs = 90000) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(NpChannel), typeof(NpEndPoint), npAddress, serializer, compressor, 
                identity, identityKey, log, stats, invokeTimeoutMs);
        }
    }
}
