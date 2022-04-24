namespace ServiceWire
{
    public interface ICompressor
    {
        byte[] Compress(byte[] data);
        byte[] DeCompress(byte[] compressedBytes);
    }
}