#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire.ZeroKnowledge
{
    public class ZkPasswordHash
    {
        #region  Proporties

        public byte[] Salt { get; set; }
        public byte[] Key { get; set; }
        public byte[] Verifier { get; set; }

        #endregion
    }
}