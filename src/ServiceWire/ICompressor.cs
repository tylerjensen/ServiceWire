namespace ServiceWire
{
    public interface ICompressor
    {
        public byte[] Compress(byte[] data);
        public byte[] DeCompress(byte[] compressedBytes);
    }
}