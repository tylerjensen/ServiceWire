#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpProxy
    {
        #region Methods


        #region Public Methods

        public static TInterface CreateProxy<TInterface>(NpEndPoint npAddress) where TInterface : class
        {
            return ProxyFactory.CreateProxy<TInterface>(typeof(NpChannel),typeof(NpEndPoint),npAddress);
        }

        #endregion


        #endregion
    }
}