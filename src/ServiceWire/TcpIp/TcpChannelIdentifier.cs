using System;
using System.Net;

namespace ServiceWire.TcpIp
{
    internal class TcpChannelIdentifier : IChannelIdentifier, IEquatable<TcpChannelIdentifier>
    {
        public string IpAddressAndPort { get; private set; }

        public TcpChannelIdentifier(IPEndPoint ipEndpoint)
        {
            IpAddressAndPort = ipEndpoint.ToString();
        }

        public bool Equals(TcpChannelIdentifier other)
        {
            return IpAddressAndPort == other.IpAddressAndPort;
        }

        public override bool Equals(object obj)
        {
            if (obj is TcpChannelIdentifier other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return IpAddressAndPort.GetHashCode();
        }
    }
}
