using System;

namespace ServiceWire
{
    [Serializable]
    public class MemoryDetail
    {
        public ulong TotalVisibleMemorySize { get; set; }
        public ulong TotalVirtualMemorySize { get; set; }
        public ulong FreePhysicalMemory { get; set; }
        public ulong FreeVirtualMemory { get; set; }
    }
}