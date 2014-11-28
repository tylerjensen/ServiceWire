using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpZkEndPoint
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public int ConnectTimeOutMs { get; set; }

        public TcpZkEndPoint(string username, string password, IPEndPoint endPoint, int connectTimeOutMs = 2500)
        {
            this.Username = username;
            this.Password = password;
            this.EndPoint = endPoint;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }
    }
}
