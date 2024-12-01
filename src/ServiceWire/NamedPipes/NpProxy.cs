namespace ServiceWire.NamedPipes
{
    public class NpProxy
    {
        public static TInterface CreateProxy<TInterface>(NpEndPoint npAddress, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(NpChannel), typeof(NpEndPoint), npAddress, serializer, compressor, logger, stats);
        }
    }
}
