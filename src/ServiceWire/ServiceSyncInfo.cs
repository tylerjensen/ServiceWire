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
    }
}