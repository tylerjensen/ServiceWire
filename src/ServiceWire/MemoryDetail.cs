#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    public class MemoryDetail
    {
        #region  Proporties

        public ulong TotalVisibleMemorySize { get; set; }
        public ulong TotalVirtualMemorySize { get; set; }
        public ulong FreePhysicalMemory { get; set; }
        public ulong FreeVirtualMemory { get; set; }

        #endregion
    }
}