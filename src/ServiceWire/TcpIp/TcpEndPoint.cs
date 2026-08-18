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

        /// <summary>
        /// When true (the default) the client uses the v2 wire protocol when the
        /// server advertises it: framed messages with correlation ids that allow
        /// concurrent in-flight calls on a shared proxy and survive request decode
        /// errors, at a small fixed cost per call. Set false to force the classic
        /// v1 wire (lowest per-call overhead for strictly sequential callers).
        /// </summary>
        public bool UseWireV2 { get; set; } = true;

        public TcpEndPoint(IPEndPoint endPoint, int connectTimeOutMs = 2500)
        {
            this.EndPoint = endPoint;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }
    }
}
