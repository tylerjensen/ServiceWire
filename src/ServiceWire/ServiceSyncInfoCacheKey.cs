using System;

namespace ServiceWire
{
    internal class ServiceSyncInfoCacheKey : IEquatable<ServiceSyncInfoCacheKey>
    {
        public Type Type { get; }
        public IChannelIdentifier ChannelIdentifier { get; }

        public ServiceSyncInfoCacheKey(Type type, IChannelIdentifier channelIdentifier)
        {
            Type = type;
            ChannelIdentifier = channelIdentifier;
        }

        public bool Equals(ServiceSyncInfoCacheKey other)
        {
            return Type.Equals(other.Type)
                && ChannelIdentifier.Equals(other.ChannelIdentifier);
        }

        public override bool Equals(object obj)
        {
            if (obj is ServiceSyncInfoCacheKey other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Type.GetHashCode() + ChannelIdentifier.GetHashCode();
            }
        }
    }
}
