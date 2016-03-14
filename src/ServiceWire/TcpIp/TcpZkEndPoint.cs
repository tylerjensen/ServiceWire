#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System.Net;

#endregion


namespace ServiceWire.TcpIp
{
    public class TcpZkEndPoint
    {
        #region Constractor

        public TcpZkEndPoint(string username,string password,IPEndPoint endPoint,int connectTimeOutMs=2500)
        {
            Username=username;
            Password=password;
            EndPoint=endPoint;
            ConnectTimeOutMs=connectTimeOutMs;
        }

        #endregion


        #region  Proporties

        public string Username { get; set; }
        public string Password { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public int ConnectTimeOutMs { get; set; }

        #endregion
    }
}