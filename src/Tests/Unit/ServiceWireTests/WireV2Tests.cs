using ServiceWire;
using ServiceWire.TcpIp;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ServiceWireTests
{
    public interface IV2Tester
    {
        int Ping();
        DateTime Echo(DateTime value);
        DateTime[] EchoArray(DateTime[] values);
    }

    public class V2Tester : IV2Tester
    {
        public int Ping() => 42;
        public DateTime Echo(DateTime value) => value;
        public DateTime[] EchoArray(DateTime[] values) => values;
    }

    public class WireV2Tests
    {
        private static readonly DateTime FixedUtc = new DateTime(2024, 12, 5, 10, 30, 15, 123, DateTimeKind.Utc);
        private static readonly DateTime FixedLocal = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Local);

        [Fact]
        public void V2Encoding_DateTime_UsesBinaryCodes()
        {
            var pth = new ParameterTransferHelper(new DefaultSerializer(), new DefaultCompressor());
            byte[] actual;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                pth.SendParameters(false, 0, w, WireVersion.V2,
                    new object[] { FixedUtc, new DateTime[] { FixedUtc, FixedLocal } });
                w.Flush();
                actual = ms.ToArray();
            }

            byte[] expected;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(2);
                w.Write((byte)0x15); w.Write(FixedUtc.ToBinary());
                w.Write((byte)0x55); w.Write(2); w.Write(FixedUtc.ToBinary()); w.Write(FixedLocal.ToBinary());
                w.Flush();
                expected = ms.ToArray();
            }
            Assert.Equal(expected, actual);

            //and it round-trips through the v2 reader with Kind preserved
            using (var ms = new MemoryStream(actual))
            using (var r = new BinaryReader(ms))
            {
                var restored = pth.ReceiveParameters(r, WireVersion.V2);
                Assert.Equal(FixedUtc, (DateTime)restored[0]);
                Assert.Equal(DateTimeKind.Utc, ((DateTime)restored[0]).Kind);
                var arr = (DateTime[])restored[1];
                Assert.Equal(FixedLocal, arr[1]);
                Assert.Equal(DateTimeKind.Local, arr[1].Kind);
            }
        }

        [Fact]
        public void V2Encoding_NonDateTimeParameters_AreUnchangedFromV1()
        {
            var pth = new ParameterTransferHelper(new DefaultSerializer(), new DefaultCompressor());
            var parameters = new object[] { 5, "text", new int[] { 1, 2, 3 }, Guid.Empty, null };
            byte[] v1, v2;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                pth.SendParameters(false, 0, w, WireVersion.V1, parameters);
                w.Flush();
                v1 = ms.ToArray();
            }
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                pth.SendParameters(false, 0, w, WireVersion.V2, parameters);
                w.Flush();
                v2 = ms.ToArray();
            }
            Assert.Equal(v1, v2);
        }

        [Fact]
        public void V1Reader_RejectsV2TypeCodes()
        {
            var pth = new ParameterTransferHelper(new DefaultSerializer(), new DefaultCompressor());
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(1);
                w.Write((byte)0x15);
                w.Write(FixedUtc.ToBinary());
                w.Flush();
                ms.Position = 0;
                using (var r = new BinaryReader(ms))
                {
                    Assert.Throws<Exception>(() => pth.ReceiveParameters(r));
                }
            }
        }

        [Fact]
        public void HostWithWireV2Disabled_ServesV1Clients()
        {
            var port = TestPorts.GetFreePort();
            using (var host = new TcpHost(port))
            {
                host.EnableWireV2 = false;
                host.AddService<IV2Tester>(new V2Tester());
                host.Open();

                StreamingChannel.ClearCachedSyncInfo();
                using (var client = new TcpClient<IV2Tester>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port))))
                {
                    Assert.Equal(42, client.Proxy.Ping());
                    var echoed = client.Proxy.Echo(FixedUtc);
                    Assert.Equal(FixedUtc, echoed);
                }
            }
            StreamingChannel.ClearCachedSyncInfo();
        }

        [Fact]
        public void V2RoundTrip_PreservesDateTimeKind()
        {
            var port = TestPorts.GetFreePort();
            using (var host = new TcpHost(port))
            {
                host.AddService<IV2Tester>(new V2Tester());
                host.Open();

                StreamingChannel.ClearCachedSyncInfo();
                using (var client = new TcpClient<IV2Tester>(new TcpEndPoint(new IPEndPoint(IPAddress.Loopback, port))))
                {
                    var echoed = client.Proxy.Echo(FixedUtc);
                    Assert.Equal(FixedUtc, echoed);
                    Assert.Equal(DateTimeKind.Utc, echoed.Kind);

                    var arr = client.Proxy.EchoArray(new[] { FixedUtc, FixedUtc.AddDays(1) });
                    Assert.Equal(2, arr.Length);
                    Assert.Equal(FixedUtc.AddDays(1), arr[1]);
                }
            }
            StreamingChannel.ClearCachedSyncInfo();
        }

        [Fact]
        public void CorruptV2Frame_GetsErrorResponseAndConnectionSurvives()
        {
            var port = TestPorts.GetFreePort();
            using (var host = new TcpHost(port))
            {
                host.AddService<IV2Tester>(new V2Tester());
                host.Open();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
                    using (var stream = new NetworkStream(socket))
                    using (var writer = new BinaryWriter(stream))
                    using (var reader = new BinaryReader(stream))
                    {
                        // frame 1: valid header for Ping (service 0, method 0) but garbage payload
                        var garbage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
                        writer.Write((int)6); //MethodInvocation2
                        writer.Write(13 + garbage.Length);
                        writer.Write(101); //correlation id
                        writer.Write(0);   //service key
                        writer.Write(0);   //method ident (Ping)
                        writer.Write((byte)0);
                        writer.Write(garbage);
                        writer.Flush();

                        Assert.Equal(7, reader.ReadInt32()); //Response2
                        var frameLength = reader.ReadInt32();
                        Assert.Equal(101, reader.ReadInt32());
                        Assert.Equal(1, reader.ReadByte()); //status: exception
                        reader.ReadByte(); //flags
                        reader.ReadBytes(frameLength - 6); //consume error payload

                        // frame 2: the same connection must still serve a valid call
                        var pth = new ParameterTransferHelper(new DefaultSerializer(), new DefaultCompressor());
                        byte[] payload;
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            pth.SendParameters(false, 0, bw, WireVersion.V2, new object[0]);
                            bw.Flush();
                            payload = ms.ToArray();
                        }
                        writer.Write((int)6);
                        writer.Write(13 + payload.Length);
                        writer.Write(102);
                        writer.Write(0);
                        writer.Write(0); //Ping
                        writer.Write((byte)0);
                        writer.Write(payload);
                        writer.Flush();

                        Assert.Equal(7, reader.ReadInt32());
                        var frame2Length = reader.ReadInt32();
                        Assert.Equal(102, reader.ReadInt32());
                        Assert.Equal(0, reader.ReadByte()); //status: return values
                        reader.ReadByte();
                        var respPayload = reader.ReadBytes(frame2Length - 6);
                        using (var ms = new MemoryStream(respPayload))
                        using (var br = new BinaryReader(ms))
                        {
                            var outParams = pth.ReceiveParameters(br, WireVersion.V2);
                            Assert.Equal(42, outParams[0]);
                        }
                    }
                }
            }
        }

        [Fact]
        public void UnknownMethod_V2_ReturnsStatus2WithCorrelationId()
        {
            var port = TestPorts.GetFreePort();
            using (var host = new TcpHost(port))
            {
                host.AddService<IV2Tester>(new V2Tester());
                host.Open();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
                    using (var stream = new NetworkStream(socket))
                    using (var writer = new BinaryWriter(stream))
                    using (var reader = new BinaryReader(stream))
                    {
                        writer.Write((int)6);
                        writer.Write(13);
                        writer.Write(777);
                        writer.Write(0);
                        writer.Write(9999); //no such method
                        writer.Write((byte)0);
                        writer.Flush();

                        Assert.Equal(7, reader.ReadInt32());
                        Assert.Equal(6, reader.ReadInt32()); //header only, no payload
                        Assert.Equal(777, reader.ReadInt32());
                        Assert.Equal(2, reader.ReadByte()); //status: unknown method
                    }
                }
            }
        }
    }
}
