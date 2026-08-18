using System.Net;

namespace ServiceWire.TcpIp
{
    public class TcpEndPoint
    {
        public IPEndPoint EndPoint { get; private set; }
        public int ConnectTimeOutMs { get; private set; }

        /// <summary>
        /// Socket receive timeout in milliseconds applied to the client socket.
        /// Default 0 means infinite (the pre-7.0 behavior).
        /// </summary>
        public int ReceiveTimeoutMs { get; set; }

        /// <summary>
        /// Socket send timeout in milliseconds applied to the client socket.
        /// Default 0 means infinite (the pre-7.0 behavior).
        /// </summary>
        public int SendTimeoutMs { get; set; }

        public TcpEndPoint(IPEndPoint endPoint, int connectTimeOutMs = 2500)
        {
            this.EndPoint = endPoint;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }
    }
}
