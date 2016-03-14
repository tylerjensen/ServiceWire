#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    public class ServiceSyncInfo
    {
        #region  Proporties

        public int ServiceKeyIndex { get; set; }
        public MethodSyncInfo[] MethodInfos { get; set; }
        public bool UseCompression { get; set; }
        public int CompressionThreshold { get; set; }

        #endregion
    }
}