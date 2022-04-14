namespace ServiceWire.NamedPipes
{
    public class NpProxy
    {
        public static TInterface CreateProxy<TInterface>(NpEndPoint npAddress, ISerializer serializer, ICompressor compressor) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(NpChannel), typeof(NpEndPoint), npAddress, serializer, compressor);
        }
    }
}
