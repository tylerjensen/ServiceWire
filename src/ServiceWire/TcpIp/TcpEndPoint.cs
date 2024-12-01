using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpEndPoint
    {
        public IPEndPoint EndPoint { get; private set; }
        public int ConnectTimeOutMs { get; private set; }

        public TcpEndPoint(IPEndPoint endPoint, int connectTimeOutMs = 2500)
        {
            this.EndPoint = endPoint;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }
    }
}
