using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ServiceWire
{
    public sealed class ParameterTransferHelper
    {
        private static readonly Dictionary<Type, byte> _parameterTypes = new Dictionary<Type, byte>
        {
            { typeof(bool), ParameterTypes.Bool },
            { typeof(byte), ParameterTypes.Byte },
            { typeof(sbyte), ParameterTypes.SByte },
            { typeof(char), ParameterTypes.Char },
            { typeof(decimal), ParameterTypes.Decimal },
            { typeof(double), ParameterTypes.Double },
            { typeof(float), ParameterTypes.Float },
            { typeof(int), ParameterTypes.Int },
            { typeof(uint), ParameterTypes.UInt },
            { typeof(long), ParameterTypes.Long },
            { typeof(ulong), ParameterTypes.ULong },
            { typeof(short), ParameterTypes.Short },
            { typeof(ushort), ParameterTypes.UShort },
            { typeof(string), ParameterTypes.String },
            { typeof(byte[]), ParameterTypes.ByteArray },
            { typeof(char[]), ParameterTypes.CharArray },
            { typeof(Type), ParameterTypes.Type },
            { typeof(Guid), ParameterTypes.Guid },
            { typeof(DateTime), ParameterTypes.DateTime },
            { typeof(bool[]), ParameterTypes.ArrayBool },
            { typeof(sbyte[]), ParameterTypes.ArraySByte },
            { typeof(decimal[]), ParameterTypes.ArrayDecimal },
            { typeof(double[]), ParameterTypes.ArrayDouble },
            { typeof(float[]), ParameterTypes.ArrayFloat },
            { typeof(int[]), ParameterTypes.ArrayInt },
            { typeof(uint[]), ParameterTypes.ArrayUInt },
            { typeof(long[]), ParameterTypes.ArrayLong },
            { typeof(ulong[]), ParameterTypes.ArrayULong },
            { typeof(short[]), ParameterTypes.ArrayShort },
            { typeof(ushort[]), ParameterTypes.ArrayUShort },
            { typeof(string[]), ParameterTypes.ArrayString },
            { typeof(Type[]), ParameterTypes.ArrayType },
            { typeof(Guid[]), ParameterTypes.ArrayGuid },
            { typeof(DateTime[]), ParameterTypes.ArrayDateTime }
        };

        private const string NULL_STRING = "\u2811\u16D8\u270C"; //3 chars from 3 different unicode sets

        private readonly ISerializer _serializer;
        private readonly ICompressor _compressor;
        public ParameterTransferHelper(ISerializer serializer, ICompressor compressor)
        {
            _serializer = serializer ?? new DefaultSerializer();
            _compressor = compressor ?? new DefaultCompressor();
        }

        public void SendParameters(bool useCompression, int compressionThreshold, BinaryWriter writer, params object[] parameters)
        {
            //write how many parameters are coming
            writer.Write(parameters.Length);
            //write data for each parameter
            foreach (object parameter in parameters)
            {
                if (parameter == null)
                {
                    writer.Write(ParameterTypes.Null);
                } else
                {
                    Type type = parameter.GetType();
                    byte typeByte = GetParameterType(type);
                    
                    byte[] dataBytes = null;

                    switch (typeByte)
                    {
                        case ParameterTypes.ByteArray:
                            dataBytes = (byte[])parameter;
                            break;
                        case ParameterTypes.Unknown:
                            dataBytes = _serializer.Serialize(parameter, type.ToConfigName());
                            break;
                    }

                    if (useCompression)
                    {
                        //check for compressable values and compress if required
                        switch (typeByte)
                        {
                            case ParameterTypes.ByteArray:
                                if (dataBytes.LongLength > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.CompressedByteArray;
                                    dataBytes = _compressor.Compress(dataBytes);
                                }
                                break;
                            case ParameterTypes.CharArray:
                                char[] charArray = (char[])parameter;
                                if (charArray.LongLength > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.CompressedCharArray;
                                    dataBytes = _compressor.Compress(Encoding.UTF8.GetBytes(charArray));
                                }
                                break;
                            case ParameterTypes.String:
                                if (((string)parameter).Length > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.CompressedString;
                                    dataBytes = _compressor.Compress(Encoding.UTF8.GetBytes(((string)parameter)));
                                }
                                break;
                            case ParameterTypes.ArrayString:
                                var array = (string[])parameter;
                                var total = (from n in array select n?.Length ?? 0).Sum();
                                if (total > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.CompressedUnknown;
                                    dataBytes = _compressor.Compress(_serializer.Serialize(array, type.ToConfigName()));
                                }
                                break;
                            case ParameterTypes.Unknown:
                                if (dataBytes.Length > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.CompressedUnknown;
                                    dataBytes = _compressor.Compress(dataBytes);
                                }
                                break;
                        }
                    }

                    //write the type byte
                    writer.Write(typeByte);
                    //write the parameter
                    switch (typeByte)
                    {
                        case ParameterTypes.Bool:
                            writer.Write((bool)parameter);
                            break;
                        case ParameterTypes.Byte:
                            writer.Write((byte)parameter);
                            break;
                        case ParameterTypes.Char:
                            writer.Write((char)parameter);
                            break;
                        case ParameterTypes.CharArray:
                            char[] charArray = (char[])parameter;
                            writer.Write(charArray.Length);
                            writer.Write(charArray);
                            break;
                        case ParameterTypes.Decimal:
                            writer.Write((decimal)parameter);
                            break;
                        case ParameterTypes.Double:
                            writer.Write((double)parameter);
                            break;
                        case ParameterTypes.Float:
                            writer.Write((float)parameter);
                            break;
                        case ParameterTypes.Int:
                            writer.Write((int)parameter);
                            break;
                        case ParameterTypes.Long:
                            writer.Write((long)parameter);
                            break;
                        case ParameterTypes.SByte:
                            writer.Write((sbyte)parameter);
                            break;
                        case ParameterTypes.Short:
                            writer.Write((short)parameter);
                            break;
                        case ParameterTypes.String:
                            writer.Write((string)parameter);
                            break;
                        case ParameterTypes.UInt:
                            writer.Write((uint)parameter);
                            break;
                        case ParameterTypes.ULong:
                            writer.Write((ulong)parameter);
                            break;
                        case ParameterTypes.UShort:
                            writer.Write((ushort)parameter);
                            break;
                        case ParameterTypes.Type:
                            //the parameter value itself is the Type being transferred
                            writer.Write(((Type)parameter).ToConfigName());
                            break;
                        case ParameterTypes.Guid:
                        {
#if NET8_0_OR_GREATER
                            Span<byte> guidSpan = stackalloc byte[16];
                            ((Guid)parameter).TryWriteBytes(guidSpan);
                            writer.Write(guidSpan);
#else
                            writer.Write(((Guid)parameter).ToByteArray());
#endif
                            break;
                        }
                        case ParameterTypes.DateTime:
                            writer.Write(((DateTime)parameter).ToString("o"));
                            break;

                        case ParameterTypes.ArrayBool:
                            var bools = (bool[])parameter;
                            writer.Write(bools.Length);
                            foreach (var b in bools) writer.Write(b);
                            break;
                        case ParameterTypes.ArraySByte:
                            var sbytes = (sbyte[])parameter;
                            writer.Write(sbytes.Length);
                            foreach (var sb in sbytes) writer.Write(sb);
                            break;
                        case ParameterTypes.ArrayDecimal:
                            var decs = (decimal[])parameter;
                            writer.Write(decs.Length);
                            foreach (var d in decs) writer.Write(d);
                            break;
                        case ParameterTypes.ArrayDouble:
                            var dbls = (double[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, dbls)) break;
#endif
                            writer.Write(dbls.Length);
                            foreach (var db in dbls) writer.Write(db);
                            break;
                        case ParameterTypes.ArrayFloat:
                            var fls = (float[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, fls)) break;
#endif
                            writer.Write(fls.Length);
                            foreach (var f in fls) writer.Write(f);
                            break;
                        case ParameterTypes.ArrayInt:
                            var ints = (int[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, ints)) break;
#endif
                            writer.Write(ints.Length);
                            foreach (var i in ints) writer.Write(i);
                            break;
                        case ParameterTypes.ArrayUInt:
                            var uints = (uint[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, uints)) break;
#endif
                            writer.Write(uints.Length);
                            foreach (var u in uints) writer.Write(u);
                            break;
                        case ParameterTypes.ArrayLong:
                            var longs = (long[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, longs)) break;
#endif
                            writer.Write(longs.Length);
                            foreach (var lg in longs) writer.Write(lg);
                            break;
                        case ParameterTypes.ArrayULong:
                            var ulongs = (ulong[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, ulongs)) break;
#endif
                            writer.Write(ulongs.Length);
                            foreach (var ul in ulongs) writer.Write(ul);
                            break;
                        case ParameterTypes.ArrayShort:
                            var shorts = (short[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, shorts)) break;
#endif
                            writer.Write(shorts.Length);
                            foreach (var s in shorts) writer.Write(s);
                            break;
                        case ParameterTypes.ArrayUShort:
                            var ushorts = (ushort[])parameter;
#if NET8_0_OR_GREATER
                            if (TryWriteBlittable(writer, ushorts)) break;
#endif
                            writer.Write(ushorts.Length);
                            foreach (var us in ushorts) writer.Write(us);
                            break;
                        case ParameterTypes.ArrayString:
                            var strings = (string[])parameter;
                            writer.Write(strings.Length);
                            foreach (var st in strings) writer.Write(st ?? NULL_STRING);
                            break;
                        case ParameterTypes.ArrayType:
                            var types = (Type[])parameter;
                            writer.Write(types.Length);
                            foreach (var t in types)
                                writer.Write(t.ToConfigName());
                            break;
                        case ParameterTypes.ArrayGuid:
                        {
                            var guids = (Guid[])parameter;
                            writer.Write(guids.Length);
#if NET8_0_OR_GREATER
                            Span<byte> guidBuf = stackalloc byte[16];
                            foreach (var g in guids)
                            {
                                g.TryWriteBytes(guidBuf);
                                writer.Write(guidBuf);
                            }
#else
                            foreach (var g in guids) writer.Write(g.ToByteArray());
#endif
                            break;
                        }
                        case ParameterTypes.ArrayDateTime:
                            var dts = (DateTime[])parameter;
                            writer.Write(dts.Length);
                            foreach (var dt in dts) writer.Write(dt.ToString("o"));
                            break;

                        case ParameterTypes.ByteArray:
                        case ParameterTypes.CompressedByteArray:
                        case ParameterTypes.CompressedCharArray:
                        case ParameterTypes.CompressedString:
                            //write length of data
                            writer.Write(dataBytes.Length);
                            //write data
                            writer.Write(dataBytes);
                            break;
                        case ParameterTypes.Unknown:
                        case ParameterTypes.CompressedUnknown:
                            //write type name as string
                            writer.Write(type.ToConfigName());
                            //write length of data
                            writer.Write(dataBytes.Length);
                            //write data
                            writer.Write(dataBytes);
                            break;
                        default:
                            throw new Exception(string.Format("Unknown type byte '0x{0:X}'", typeByte));
                    }
                }
            }
        }

        public object[] ReceiveParameters(BinaryReader reader)
        {
            int parameterCount = reader.ReadInt32();
            object[] parameters = new object[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                //read type byte
                byte typeByte = reader.ReadByte();
                if (typeByte == ParameterTypes.Null)
                {
                    parameters[i] = null;
                } else
                {
                    switch (typeByte)
                    {
                        case ParameterTypes.Bool:
                            parameters[i] = reader.ReadBoolean();
                            break;
                        case ParameterTypes.Byte:
                            parameters[i] = reader.ReadByte();
                            break;
                        case ParameterTypes.ByteArray:
                            parameters[i] = reader.ReadBytes(reader.ReadInt32());
                            break;
                        case ParameterTypes.CompressedByteArray:
                            parameters[i] = _compressor.DeCompress(reader.ReadBytes(reader.ReadInt32()));
                            break;
                        case ParameterTypes.Char:
                            parameters[i] = reader.ReadChar();
                            break;
                        case ParameterTypes.CharArray:
                            parameters[i] = reader.ReadChars(reader.ReadInt32());
                            break;
                        case ParameterTypes.CompressedCharArray:
                            var ccBytes = _compressor.DeCompress(reader.ReadBytes(reader.ReadInt32()));
                            parameters[i] = Encoding.UTF8.GetChars(ccBytes);
                            break;
                        case ParameterTypes.Decimal:
                            parameters[i] = reader.ReadDecimal();
                            break;
                        case ParameterTypes.Double:
                            parameters[i] = reader.ReadDouble();
                            break;
                        case ParameterTypes.Float:
                            parameters[i] = reader.ReadSingle();
                            break;
                        case ParameterTypes.Int:
                            parameters[i] = reader.ReadInt32();
                            break;
                        case ParameterTypes.Long:
                            parameters[i] = reader.ReadInt64();
                            break;
                        case ParameterTypes.SByte:
                            parameters[i] = reader.ReadSByte();
                            break;
                        case ParameterTypes.Short:
                            parameters[i] = reader.ReadInt16();
                            break;
                        case ParameterTypes.String:
                            parameters[i] = reader.ReadString();
                            break;
                        case ParameterTypes.CompressedString:
                            var csBytes = _compressor.DeCompress(reader.ReadBytes(reader.ReadInt32()));
                            parameters[i] = Encoding.UTF8.GetString(csBytes);
                            break;
                        case ParameterTypes.UInt:
                            parameters[i] = reader.ReadUInt32();
                            break;
                        case ParameterTypes.ULong:
                            parameters[i] = reader.ReadUInt64();
                            break;
                        case ParameterTypes.UShort:
                            parameters[i] = reader.ReadUInt16();
                            break;
                        case ParameterTypes.Type:
                            var typeName = reader.ReadString();
                            parameters[i] = typeName.ToType();
                            break;
                        case ParameterTypes.Guid:
                            parameters[i] = new Guid(reader.ReadBytes(16));
                            break;
                        case ParameterTypes.DateTime:
                            var dtstr = reader.ReadString();
                            parameters[i] = DateTime.Parse(dtstr, null, DateTimeStyles.RoundtripKind);
                            break;

                        case ParameterTypes.ArrayBool:
                            var blen = reader.ReadInt32();
                            var bs = new bool[blen];
                            for (int x = 0; x < blen; x++) bs[x] = reader.ReadBoolean();
                            parameters[i] = bs;
                            break;
                        case ParameterTypes.ArraySByte:
                            var sblen = reader.ReadInt32();
                            var sbs = new sbyte[sblen];
                            for (int x = 0; x < sblen; x++) sbs[x] = reader.ReadSByte();
                            parameters[i] = sbs;
                            break;
                        case ParameterTypes.ArrayDecimal:
                            var dclen = reader.ReadInt32();
                            var dcs = new decimal[dclen];
                            for (int x = 0; x < dclen; x++) dcs[x] = reader.ReadDecimal();
                            parameters[i] = dcs;
                            break;
                        case ParameterTypes.ArrayDouble:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out double[] dbsFast)) { parameters[i] = dbsFast; break; }
#endif
                            var dblen = reader.ReadInt32();
                            var dbs = new double[dblen];
                            for (int x = 0; x < dblen; x++) dbs[x] = reader.ReadDouble();
                            parameters[i] = dbs;
                            break;
                        case ParameterTypes.ArrayFloat:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out float[] fsFast)) { parameters[i] = fsFast; break; }
#endif
                            var flen = reader.ReadInt32();
                            var fs = new float[flen];
                            for (int x = 0; x < flen; x++) fs[x] = reader.ReadSingle();
                            parameters[i] = fs;
                            break;
                        case ParameterTypes.ArrayInt:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out int[] issFast)) { parameters[i] = issFast; break; }
#endif
                            var ilen = reader.ReadInt32();
                            var iss = new int[ilen];
                            for (int x = 0; x < ilen; x++) iss[x] = reader.ReadInt32();
                            parameters[i] = iss;
                            break;
                        case ParameterTypes.ArrayUInt:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out uint[] uisFast)) { parameters[i] = uisFast; break; }
#endif
                            var uilen = reader.ReadInt32();
                            var uis = new uint[uilen];
                            for (int x = 0; x < uilen; x++) uis[x] = reader.ReadUInt32();
                            parameters[i] = uis;
                            break;
                        case ParameterTypes.ArrayLong:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out long[] lsFast)) { parameters[i] = lsFast; break; }
#endif
                            var llen = reader.ReadInt32();
                            var ls = new long[llen];
                            for (int x = 0; x < llen; x++) ls[x] = reader.ReadInt64();
                            parameters[i] = ls;
                            break;
                        case ParameterTypes.ArrayULong:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out ulong[] ulsFast)) { parameters[i] = ulsFast; break; }
#endif
                            var ullen = reader.ReadInt32();
                            var uls = new ulong[ullen];
                            for (int x = 0; x < ullen; x++) uls[x] = reader.ReadUInt64();
                            parameters[i] = uls;
                            break;
                        case ParameterTypes.ArrayShort:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out short[] sssFast)) { parameters[i] = sssFast; break; }
#endif
                            var sslen = reader.ReadInt32();
                            var sss = new short[sslen];
                            for (int x = 0; x < sslen; x++) sss[x] = reader.ReadInt16();
                            parameters[i] = sss;
                            break;
                        case ParameterTypes.ArrayUShort:
#if NET8_0_OR_GREATER
                            if (TryReadBlittable(reader, out ushort[] usFast)) { parameters[i] = usFast; break; }
#endif
                            var ulen = reader.ReadInt32();
                            var us = new ushort[ulen];
                            for (int x = 0; x < ulen; x++) us[x] = reader.ReadUInt16();
                            parameters[i] = us;
                            break;
                        case ParameterTypes.ArrayString:
                            var slen = reader.ReadInt32();
                            var ss = new string[slen];
                            for (int x = 0; x < slen; x++)
                            {
                                ss[x] = reader.ReadString();
                                if (ss[x] == NULL_STRING) ss[x] = null;
                            }
                            parameters[i] = ss;
                            break;
                        case ParameterTypes.ArrayType:
                            var tlen = reader.ReadInt32();
                            var ts = new Type[tlen];
                            for (int x = 0; x < tlen; x++) ts[x] = reader.ReadString().ToType();
                            parameters[i] = ts;
                            break;
                        case ParameterTypes.ArrayGuid:
                            var glen = reader.ReadInt32();
                            var gs = new Guid[glen];
                            for (int x = 0; x < glen; x++) gs[x] = new Guid(reader.ReadBytes(16));
                            parameters[i] = gs;
                            break;
                        case ParameterTypes.ArrayDateTime:
                            var dlen = reader.ReadInt32();
                            var dts = new DateTime[dlen];
                            for (int x = 0; x < dlen; x++)
                            {
                                var adtstr = reader.ReadString();
                                dts[x] = DateTime.Parse(adtstr, null, DateTimeStyles.RoundtripKind);
                            }
                            parameters[i] = dts;
                            break;

                        case ParameterTypes.Unknown:
                            var typeConfigName = reader.ReadString();
                            var bytes = reader.ReadBytes(reader.ReadInt32());
                            parameters[i] = _serializer.Deserialize(bytes, typeConfigName);
                            break;
                        case ParameterTypes.CompressedUnknown:
                            var cuTypeConfigName = reader.ReadString();
                            var cuBytes = _compressor.DeCompress(reader.ReadBytes(reader.ReadInt32()));
                            parameters[i] = _serializer.Deserialize(cuBytes, cuTypeConfigName);
                            break;
                        default:
                            throw new Exception(string.Format("Unknown type byte '0x{0:X}'", typeByte));
                    }
                }
            }
            return parameters;
        }

#if NET8_0_OR_GREATER
        //bulk copies write the same little-endian bytes the per-element BinaryWriter
        //calls produce, so the wire format is unchanged; big-endian platforms (and
        //older TFMs) keep the per-element loops
        private static bool TryWriteBlittable<T>(BinaryWriter writer, T[] array) where T : unmanaged
        {
            if (!BitConverter.IsLittleEndian) return false;
            writer.Write(array.Length);
            writer.Write(MemoryMarshal.AsBytes(array.AsSpan()));
            return true;
        }

        private static bool TryReadBlittable<T>(BinaryReader reader, out T[] array) where T : unmanaged
        {
            if (!BitConverter.IsLittleEndian)
            {
                array = null;
                return false;
            }
            var len = reader.ReadInt32();
            array = new T[len];
            if (len > 0) reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(array.AsSpan()));
            return true;
        }
#endif

        private byte GetParameterType(Type type)
        {
            byte parameterType;
            if (_parameterTypes.TryGetValue(type, out parameterType))
                return parameterType;
            //a Type value's runtime type is RuntimeType (and a Type[] created by
            //reflection can be RuntimeType[]), which never matches the exact-type
            //map above; map them to the Type codes the wire format already defines
            if (typeof(Type).IsAssignableFrom(type))
                return ParameterTypes.Type;
            if (type.IsArray && typeof(Type).IsAssignableFrom(type.GetElementType()))
                return ParameterTypes.ArrayType;
            return ParameterTypes.Unknown;
        }
    }
}
