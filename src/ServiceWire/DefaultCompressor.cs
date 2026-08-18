using System.IO;
using System.IO.Compression;

namespace ServiceWire
{
    public class DefaultCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            using (var msCompressed = new MemoryStream())
            {
                using (GZipStream gzs = new GZipStream(msCompressed, CompressionMode.Compress))
                {
                    gzs.Write(data, 0, data.Length);
                }
                return msCompressed.ToArray();
            }
        }
        public byte[] DeCompress(byte[] compressedBytes)
        {
            using (var msObj = new MemoryStream())
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
