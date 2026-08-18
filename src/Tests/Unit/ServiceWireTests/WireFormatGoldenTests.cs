using ServiceWire;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace ServiceWireTests
{
    /// <summary>
    /// Golden tests that lock the v1 parameter-block wire format byte for byte.
    /// Expected bytes are produced by an independent reference encoder that follows
    /// the documented frame layout, never by the production code under test.
    /// Any change that alters these bytes is a wire protocol break for existing peers.
    /// </summary>
    public class WireFormatGoldenTests
    {
        private static readonly Guid FixedGuid = new Guid("a1b2c3d4-e5f6-4a5b-8c7d-9e0f1a2b3c4d");
        private static readonly DateTime FixedUtc = new DateTime(2024, 12, 5, 10, 30, 15, 123, DateTimeKind.Utc);
        private static readonly DateTime FixedUnspecified = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        private static byte[] Send(bool useCompression, int threshold, params object[] parameters)
        {
            var pth = new ParameterTransferHelper(new DefaultSerializer(), new DefaultCompressor());
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                pth.SendParameters(useCompression, threshold, writer, parameters);
                writer.Flush();
                return ms.ToArray();
            }
        }

        private static byte[] Expected(Action<BinaryWriter> write)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                write(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        [Fact]
        public void ScalarPrimitives_MatchGoldenBytes()
        {
            var actual = Send(false, 0,
                true, (byte)0x32, (sbyte)-33, 'c', 1.34m, 3.45d, 32.3f,
                34588, 34444u, 3499887766L, 9876758765654UL, (short)234, (ushort)888,
                "hello");

            var expected = Expected(w =>
            {
                w.Write(14);
                w.Write((byte)0x01); w.Write(true);
                w.Write((byte)0x02); w.Write((byte)0x32);
                w.Write((byte)0x03); w.Write((sbyte)-33);
                w.Write((byte)0x04); w.Write('c');
                w.Write((byte)0x05); w.Write(1.34m);
                w.Write((byte)0x06); w.Write(3.45d);
                w.Write((byte)0x07); w.Write(32.3f);
                w.Write((byte)0x08); w.Write(34588);
                w.Write((byte)0x09); w.Write(34444u);
                w.Write((byte)0x0A); w.Write(3499887766L);
                w.Write((byte)0x0B); w.Write(9876758765654UL);
                w.Write((byte)0x0C); w.Write((short)234);
                w.Write((byte)0x0D); w.Write((ushort)888);
                w.Write((byte)0x0E); w.Write("hello");
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NullGuidAndDateTime_MatchGoldenBytes()
        {
            var actual = Send(false, 0, null, FixedGuid, FixedUtc, FixedUnspecified);

            var expected = Expected(w =>
            {
                w.Write(4);
                w.Write((byte)0x11); // null
                w.Write((byte)0x13); w.Write(FixedGuid.ToByteArray()); // 16 raw bytes
                w.Write((byte)0x14); w.Write("2024-12-05T10:30:15.1230000Z");
                w.Write((byte)0x14); w.Write("2023-01-02T03:04:05.0000000");
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ByteAndCharArrays_MatchGoldenBytes()
        {
            var bytes = new byte[] { 0x01, 0x02, 0xFF };
            var chars = new char[] { 'a', 'b', 'c', 'd' };
            var actual = Send(false, 0, bytes, chars);

            var expected = Expected(w =>
            {
                w.Write(2);
                w.Write((byte)0x0F); w.Write(3); w.Write(bytes);          // ByteArray: len + data
                w.Write((byte)0x10); w.Write(4); w.Write(chars);          // CharArray: count + chars
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void PrimitiveArrays_MatchGoldenBytes()
        {
            var actual = Send(false, 0,
                new bool[] { true, false },
                new sbyte[] { -1, 2 },
                new decimal[] { 1.5m },
                new double[] { 2.5d },
                new float[] { 3.5f },
                new int[] { 7, 8, 9 },
                new uint[] { 10u },
                new long[] { -11L },
                new ulong[] { 12UL },
                new short[] { -13 },
                new ushort[] { 14 });

            var expected = Expected(w =>
            {
                w.Write(11);
                w.Write((byte)0x41); w.Write(2); w.Write(true); w.Write(false);
                w.Write((byte)0x43); w.Write(2); w.Write((sbyte)-1); w.Write((sbyte)2);
                w.Write((byte)0x45); w.Write(1); w.Write(1.5m);
                w.Write((byte)0x46); w.Write(1); w.Write(2.5d);
                w.Write((byte)0x47); w.Write(1); w.Write(3.5f);
                w.Write((byte)0x48); w.Write(3); w.Write(7); w.Write(8); w.Write(9);
                w.Write((byte)0x49); w.Write(1); w.Write(10u);
                w.Write((byte)0x4A); w.Write(1); w.Write(-11L);
                w.Write((byte)0x4B); w.Write(1); w.Write(12UL);
                w.Write((byte)0x4C); w.Write(1); w.Write((short)-13);
                w.Write((byte)0x4D); w.Write(1); w.Write((ushort)14);
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StringTypeGuidDateTimeArrays_MatchGoldenBytes()
        {
            var actual = Send(false, 0,
                new string[] { "one", null, "three" },
                new Type[] { typeof(string), typeof(Guid) },
                new Guid[] { FixedGuid, FixedGuid },
                new DateTime[] { FixedUtc, FixedUnspecified });

            var expected = Expected(w =>
            {
                w.Write(4);
                w.Write((byte)0x4E); w.Write(3);
                w.Write("one");
                w.Write("⠑ᛘ✌"); // null sentinel
                w.Write("three");
                w.Write((byte)0x52); w.Write(2); w.Write("System.String"); w.Write("System.Guid");
                w.Write((byte)0x53); w.Write(2); w.Write(FixedGuid.ToByteArray()); w.Write(FixedGuid.ToByteArray());
                w.Write((byte)0x54); w.Write(2); w.Write("2024-12-05T10:30:15.1230000Z"); w.Write("2023-01-02T03:04:05.0000000");
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ComplexType_Uncompressed_MatchGoldenBytes()
        {
            var complex = new GoldenComplex { Id = 42, Name = "golden" };
            var actual = Send(false, 0, complex);

            var serialized = new DefaultSerializer().Serialize(complex, typeof(GoldenComplex).ToConfigName());
            var expected = Expected(w =>
            {
                w.Write(1);
                w.Write((byte)0x00); // Unknown
                w.Write("ServiceWireTests.WireFormatGoldenTests+GoldenComplex, ServiceWireTests");
                w.Write(serialized.Length);
                w.Write(serialized);
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CompressedByteArray_HasExpectedFrameAndRoundTrips()
        {
            var data = new byte[2048];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)(i % 7);
            var actual = Send(true, 1024, data);

            using (var ms = new MemoryStream(actual))
            using (var r = new BinaryReader(ms))
            {
                Assert.Equal(1, r.ReadInt32());
                Assert.Equal(0x20, r.ReadByte()); // CompressedByteArray
                var len = r.ReadInt32();
                var payload = r.ReadBytes(len);
                Assert.Equal(ms.Length, ms.Position); // frame fully consumed
                Assert.Equal(data, new DefaultCompressor().DeCompress(payload));
            }
        }

        [Fact]
        public void CompressedCharArray_HasExpectedFrameAndRoundTrips()
        {
            var data = new string('x', 2048).ToCharArray();
            var actual = Send(true, 1024, data);

            using (var ms = new MemoryStream(actual))
            using (var r = new BinaryReader(ms))
            {
                Assert.Equal(1, r.ReadInt32());
                Assert.Equal(0x21, r.ReadByte()); // CompressedCharArray
                var len = r.ReadInt32();
                var payload = r.ReadBytes(len);
                Assert.Equal(ms.Length, ms.Position);
                Assert.Equal(data, Encoding.UTF8.GetChars(new DefaultCompressor().DeCompress(payload)));
            }
        }

        [Fact]
        public void CompressedString_HasExpectedFrameAndRoundTrips()
        {
            var data = new string('y', 4096);
            var actual = Send(true, 1024, data);

            using (var ms = new MemoryStream(actual))
            using (var r = new BinaryReader(ms))
            {
                Assert.Equal(1, r.ReadInt32());
                Assert.Equal(0x22, r.ReadByte()); // CompressedString
                var len = r.ReadInt32();
                var payload = r.ReadBytes(len);
                Assert.Equal(ms.Length, ms.Position);
                Assert.Equal(data, Encoding.UTF8.GetString(new DefaultCompressor().DeCompress(payload)));
            }
        }

        [Fact]
        public void CompressedComplexType_HasExpectedFrameAndRoundTrips()
        {
            var complex = new GoldenComplex { Id = 7, Name = new string('z', 4096) };
            var actual = Send(true, 1024, complex);

            using (var ms = new MemoryStream(actual))
            using (var r = new BinaryReader(ms))
            {
                Assert.Equal(1, r.ReadInt32());
                Assert.Equal(0x23, r.ReadByte()); // CompressedUnknown
                Assert.Equal("ServiceWireTests.WireFormatGoldenTests+GoldenComplex, ServiceWireTests", r.ReadString());
                var len = r.ReadInt32();
                var payload = r.ReadBytes(len);
                Assert.Equal(ms.Length, ms.Position);
                var restored = (GoldenComplex)new DefaultSerializer().Deserialize(
                    new DefaultCompressor().DeCompress(payload), typeof(GoldenComplex).ToConfigName());
                Assert.Equal(complex.Id, restored.Id);
                Assert.Equal(complex.Name, restored.Name);
            }
        }

        [Fact]
        public void BelowThreshold_CompressionEnabled_LeavesFormatUnchanged()
        {
            var data = new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var actual = Send(true, 1024, data, "short", new string('c', 8).ToCharArray());

            var expected = Expected(w =>
            {
                w.Write(3);
                w.Write((byte)0x0F); w.Write(16); w.Write(data);
                w.Write((byte)0x0E); w.Write("short");
                w.Write((byte)0x10); w.Write(8); w.Write(new string('c', 8).ToCharArray());
            });

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ConfigNames_MatchGoldenStrings()
        {
            // These exact strings ride the wire; they must never change for existing types.
            Assert.Equal("System.Int32", typeof(int).ToConfigName());
            Assert.Equal("System.String", typeof(string).ToConfigName());
            Assert.Equal("System.DateTime", typeof(DateTime).ToConfigName());
            Assert.Equal("System.Int32[]", typeof(int[]).ToConfigName());
            Assert.Equal("System.Collections.Generic.List`1[[System.Int32]]", typeof(System.Collections.Generic.List<int>).ToConfigName());
            Assert.Equal("ServiceWire.ParameterTransferHelper, ServiceWire", typeof(ParameterTransferHelper).ToConfigName());
            Assert.Equal(
                "System.Collections.Generic.List`1[[ServiceWire.ParameterTransferHelper, ServiceWire]]",
                typeof(System.Collections.Generic.List<ParameterTransferHelper>).ToConfigName());
        }

        [Fact]
        public void ConfigNames_ResolveBackToTypes()
        {
            Assert.Equal(typeof(int), "System.Int32".ToType());
            Assert.Equal(typeof(ParameterTransferHelper), typeof(ParameterTransferHelper).ToConfigName().ToType());
            Assert.Equal(typeof(System.Collections.Generic.List<ParameterTransferHelper>),
                typeof(System.Collections.Generic.List<ParameterTransferHelper>).ToConfigName().ToType());
            Assert.Null("No.Such.Type, NoSuchAssembly".ToType());
        }

        [Serializable]
        public class GoldenComplex
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
