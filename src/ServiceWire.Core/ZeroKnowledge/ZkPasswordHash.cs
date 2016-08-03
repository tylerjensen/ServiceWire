namespace ServiceWire.ZeroKnowledge
{
    public class ZkPasswordHash
    {
        public byte[] Salt { get; set; }
        public byte[] Key { get; set; }
        public byte[] Verifier { get; set; }
    }
}