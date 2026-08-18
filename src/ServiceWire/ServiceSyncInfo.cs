using System;
using System.Runtime.Serialization;

namespace ServiceWire
{
    [Serializable, DataContract]
    public class ServiceSyncInfo
    {
        [DataMember(Order = 1)]
        public int ServiceKeyIndex { get; set; }
        [DataMember(Order = 2)]
        public MethodSyncInfo[] MethodInfos { get; set; }
        [DataMember(Order = 3)]
        public bool UseCompression { get; set; }
        [DataMember(Order = 4)]
        public int CompressionThreshold { get; set; }

        /// <summary>
        /// Bit flags of <see cref="ProtocolCapabilities"/> the server supports.
        /// Deserializes to 0 (no capabilities) when talking to a pre-7.0 server,
        /// which keeps the client on the v1 wire path. Declared as int so every
        /// injected serializer treats it as a plain scalar.
        /// </summary>
        [DataMember(Order = 5)]
        public int CapabilityFlags { get; set; }
    }
}