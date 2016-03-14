#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire.NamedPipes
{
    public class NpEndPoint
    {
        #region Constractor

        public NpEndPoint(string pipeName,int connectTimeOutMs=2500):this(".",pipeName,connectTimeOutMs)
        {
        }

        public NpEndPoint(string serverName,string pipeName,int connectTimeOutMs=2500)
        {
            ServerName=serverName;
            PipeName=pipeName;
            ConnectTimeOutMs=connectTimeOutMs;
        }

        #endregion


        #region  Proporties

        public string ServerName { get; set; }
        public string PipeName { get; set; }
        public int ConnectTimeOutMs { get; set; }

        #endregion
    }
}