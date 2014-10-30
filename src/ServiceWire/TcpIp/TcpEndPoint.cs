using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ServiceWire.TcpIp
{
    public class TcpEndPoint
    {
        public IPEndPoint EndPoint { get; set; }
        public int ConnectTimeOutMs { get; set; }

        public TcpEndPoint(IPEndPoint endPoint, int connectTimeOutMs = 2500)
        {
            this.EndPoint = endPoint;
            this.ConnectTimeOutMs = connectTimeOutMs;
        }
    }
}
