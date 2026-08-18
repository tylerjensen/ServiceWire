using System.IO;
using System.IO.Compression;

namespace ServiceWire
{
    public class DefaultCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            //gzip is self-describing, so compression level is a local choice:
            //Fastest trades a few percent of ratio for much lower latency
            using (var msCompressed = new MemoryStream(data.Length))
            {
                using (GZipStream gzs = new GZipStream(msCompressed, CompressionLevel.Fastest))
                {
                    gzs.Write(data, 0, data.Length);
                }
                return msCompressed.ToArray();
            }
        }
        public byte[] DeCompress(byte[] compressedBytes)
        {
            using (var msObj = new MemoryStream(compressedBytes.Length * 2))
            {
                using (var msCompressed = new MemoryStream(compressedBytes))
                using (var gzs = new GZipStream(msCompressed, CompressionMode.Decompress))
                {
                    gzs.CopyTo(msObj);
                }
                return msObj.ToArray();
            }
        }
    }
}
