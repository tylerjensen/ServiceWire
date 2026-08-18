using ServiceWire;
using System.Text;
using Xunit;

namespace ServiceWireTests
{
    public class InfrastructureTests
    {
        [Fact]
        public void DefaultCompressorRoundTripsPayload()
        {
            var compressor = new DefaultCompressor();
            var payload = Encoding.UTF8.GetBytes(new string('x', 4096));

            var compressed = compressor.Compress(payload);
            var decompressed = compressor.DeCompress(compressed);

            Assert.Equal(payload, decompressed);
        }

        [Fact]
        public void PooledDictionaryReusesReleasedValue()
        {
            using (var pool = new PooledDictionary<string, object>())
            {
                var created = pool.Request("key", () => new object());
                pool.Release("key", created);

                Assert.Equal(1, pool.Count("key"));
                Assert.Same(created, pool.Request("key"));
                Assert.Equal(0, pool.Count("key"));
            }
        }
    }
}
