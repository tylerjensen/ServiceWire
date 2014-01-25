namespace ServiceWire.NamedPipes
{
    public class NpProxy
    {
        public static TInterface CreateProxy<TInterface>(NpEndPoint npAddress) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(NpChannel), typeof(NpEndPoint), npAddress);
        }
    }
}
