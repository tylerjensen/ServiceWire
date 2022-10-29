using System;

namespace ServiceWire.NamedPipes
{
    internal class NpChannelIdentifier : IChannelIdentifier, IEquatable<NpChannelIdentifier>
    {
        public string ServerName { get; }
        public string PipeName { get; }

        public NpChannelIdentifier(NpEndPoint npEndPoint)
        {
            ServerName = npEndPoint.ServerName;
            PipeName = npEndPoint.PipeName;
        }

        public bool Equals(NpChannelIdentifier other)
        {
            return ServerName == other.ServerName
                && PipeName == other.PipeName;
        }

        public override bool Equals(object obj)
        {
            if (obj is NpChannelIdentifier other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ServerName.GetHashCode() + PipeName.GetHashCode();
            }
        }
    }
}
