using System;

namespace ServiceWire
{
    [Serializable]
    public class ServiceSyncInfo
    {
        public int ServiceKeyIndex { get; set; }
        public MethodSyncInfo[] MethodInfos { get; set; }
        public bool UseCompression { get; set; }
        public int CompressionThreshold { get; set; }
    }
}