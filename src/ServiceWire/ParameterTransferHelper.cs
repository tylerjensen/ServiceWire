using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        #region Send

        public void SendParameters(bool useCompression, int compressionThreshold, BinaryWriter writer, params object[] parameters)
        {
            SendParameters(useCompression, compressionThreshold, writer, WireVersion.V1, parameters);
        }

        internal void SendParameters(bool useCompression, int compressionThreshold, BinaryWriter writer, WireVersion version, object[] parameters)
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
                    WriteParameter(useCompression, compressionThreshold, writer, version, parameter);
                }
            }
        }

        /// <summary>
        /// Writes one non-null parameter as a type byte followed by its value. The type
        /// byte chosen here is what the receiver switches on, so the order of steps
        /// matters: the base code is resolved first, compression may then replace it
        /// with a compressed variant, and only the final code is written.
        /// </summary>
        private void WriteParameter(bool useCompression, int compressionThreshold, BinaryWriter writer, WireVersion version, object parameter)
        {
            Type type = parameter.GetType();
            byte typeByte = ResolveTypeByte(type, version);

            //byte arrays travel as they are; a type the wire format has no code for is
            //serialized up front because compression needs the serialized length
            byte[] dataBytes = null;
            if (typeByte == ParameterTypes.ByteArray) dataBytes = (byte[])parameter;
            else if (typeByte == ParameterTypes.Unknown) dataBytes = _serializer.Serialize(parameter, type.ToConfigName());

            if (useCompression) typeByte = Compress(compressionThreshold, parameter, type, typeByte, ref dataBytes);

            //write the type byte
            writer.Write(typeByte);
            //write the parameter
            WriteValue(writer, parameter, type, typeByte, dataBytes);
        }

        private byte ResolveTypeByte(Type type, WireVersion version)
        {
            byte typeByte = GetParameterType(type);
            if (version != WireVersion.V2) return typeByte;
            //v2 peers negotiated binary DateTime encodings
            if (typeByte == ParameterTypes.DateTime) return ParameterTypes.DateTime2;
            if (typeByte == ParameterTypes.ArrayDateTime) return ParameterTypes.ArrayDateTime2;
            return typeByte;
        }

        /// <summary>
        /// Compresses the value when it exceeds the threshold, returning the compressed
        /// type code and replacing <paramref name="dataBytes"/> with the compressed
        /// payload. Values at or below the threshold are returned untouched.
        /// </summary>
        private byte Compress(int compressionThreshold, object parameter, Type type, byte typeByte, ref byte[] dataBytes)
        {
            switch (typeByte)
            {
                case ParameterTypes.ByteArray:
                    if (dataBytes.LongLength > compressionThreshold)
                    {
                        dataBytes = _compressor.Compress(dataBytes);
                        return ParameterTypes.CompressedByteArray;
                    }
                    break;
                case ParameterTypes.CharArray:
                    char[] charArray = (char[])parameter;
                    if (charArray.LongLength > compressionThreshold)
                    {
                        dataBytes = _compressor.Compress(Encoding.UTF8.GetBytes(charArray));
                        return ParameterTypes.CompressedCharArray;
                    }
                    break;
                case ParameterTypes.String:
                    if (((string)parameter).Length > compressionThreshold)
                    {
                        dataBytes = _compressor.Compress(Encoding.UTF8.GetBytes((string)parameter));
                        return ParameterTypes.CompressedString;
                    }
                    break;
                case ParameterTypes.ArrayString:
                    var array = (string[])parameter;
                    if (TotalLength(array) > compressionThreshold)
                    {
                        //CompressedUnknown, not a string-array-specific code: every
                        //release since 1.5.0 can decode it
                        dataBytes = _compressor.Compress(_serializer.Serialize(array, type.ToConfigName()));
                        return ParameterTypes.CompressedUnknown;
                    }
                    break;
                case ParameterTypes.Unknown:
                    if (dataBytes.Length > compressionThreshold)
                    {
                        dataBytes = _compressor.Compress(dataBytes);
                        return ParameterTypes.CompressedUnknown;
                    }
                    break;
            }
            return typeByte;
        }

        private static int TotalLength(string[] values)
        {
            var total = 0;
            //checked so an absurd total overflows loudly rather than wrapping negative
            //and silently skipping compression
            checked
            {
                foreach (var value in values) total += value?.Length ?? 0;
            }
            return total;
        }

        private static void WriteValue(BinaryWriter writer, object parameter, Type type, byte typeByte, byte[] dataBytes)
        {
            switch (typeByte)
            {
                case ParameterTypes.Bool: writer.Write((bool)parameter); break;
                case ParameterTypes.Byte: writer.Write((byte)parameter); break;
                case ParameterTypes.Char: writer.Write((char)parameter); break;
                case ParameterTypes.CharArray: WriteCharArray(writer, (char[])parameter); break;
                case ParameterTypes.Decimal: writer.Write((decimal)parameter); break;
                case ParameterTypes.Double: writer.Write((double)parameter); break;
                case ParameterTypes.Float: writer.Write((float)parameter); break;
                case ParameterTypes.Int: writer.Write((int)parameter); break;
                case ParameterTypes.Long: writer.Write((long)parameter); break;
                case ParameterTypes.SByte: writer.Write((sbyte)parameter); break;
                case ParameterTypes.Short: writer.Write((short)parameter); break;
                case ParameterTypes.String: writer.Write((string)parameter); break;
                case ParameterTypes.UInt: writer.Write((uint)parameter); break;
                case ParameterTypes.ULong: writer.Write((ulong)parameter); break;
                case ParameterTypes.UShort: writer.Write((ushort)parameter); break;
                //the parameter value itself is the Type being transferred
                case ParameterTypes.Type: writer.Write(((Type)parameter).ToConfigName()); break;
                case ParameterTypes.Guid: WriteGuid(writer, (Guid)parameter); break;
                case ParameterTypes.DateTime: writer.Write(((DateTime)parameter).ToString("o")); break;
                case ParameterTypes.DateTime2: writer.Write(((DateTime)parameter).ToBinary()); break;

                case ParameterTypes.ArrayBool: WriteBoolArray(writer, (bool[])parameter); break;
                case ParameterTypes.ArraySByte: WriteSByteArray(writer, (sbyte[])parameter); break;
                case ParameterTypes.ArrayDecimal: WriteDecimalArray(writer, (decimal[])parameter); break;
                case ParameterTypes.ArrayDouble: WriteDoubleArray(writer, (double[])parameter); break;
                case ParameterTypes.ArrayFloat: WriteFloatArray(writer, (float[])parameter); break;
                case ParameterTypes.ArrayInt: WriteIntArray(writer, (int[])parameter); break;
                case ParameterTypes.ArrayUInt: WriteUIntArray(writer, (uint[])parameter); break;
                case ParameterTypes.ArrayLong: WriteLongArray(writer, (long[])parameter); break;
                case ParameterTypes.ArrayULong: WriteULongArray(writer, (ulong[])parameter); break;
                case ParameterTypes.ArrayShort: WriteShortArray(writer, (short[])parameter); break;
                case ParameterTypes.ArrayUShort: WriteUShortArray(writer, (ushort[])parameter); break;
                case ParameterTypes.ArrayString: WriteStringArray(writer, (string[])parameter); break;
                case ParameterTypes.ArrayType: WriteTypeArray(writer, (Type[])parameter); break;
                case ParameterTypes.ArrayGuid: WriteGuidArray(writer, (Guid[])parameter); break;
                case ParameterTypes.ArrayDateTime: WriteDateTimeArray(writer, (DateTime[])parameter); break;
                case ParameterTypes.ArrayDateTime2: WriteDateTimeBinaryArray(writer, (DateTime[])parameter); break;

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
                    throw UnknownTypeByte(typeByte);
            }
        }

        private static void WriteGuid(BinaryWriter writer, Guid value)
        {
#if NET8_0_OR_GREATER
            Span<byte> guidSpan = stackalloc byte[16];
            value.TryWriteBytes(guidSpan);
            writer.Write(guidSpan);
#else
            writer.Write(value.ToByteArray());
#endif
        }

        private static void WriteCharArray(BinaryWriter writer, char[] values)
        {
            writer.Write(values.Length);
            writer.Write(values);
        }

        private static void WriteBoolArray(BinaryWriter writer, bool[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteSByteArray(BinaryWriter writer, sbyte[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteDecimalArray(BinaryWriter writer, decimal[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteDoubleArray(BinaryWriter writer, double[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteFloatArray(BinaryWriter writer, float[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteIntArray(BinaryWriter writer, int[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteUIntArray(BinaryWriter writer, uint[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteLongArray(BinaryWriter writer, long[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteULongArray(BinaryWriter writer, ulong[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteShortArray(BinaryWriter writer, short[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteUShortArray(BinaryWriter writer, ushort[] values)
        {
#if NET8_0_OR_GREATER
            if (TryWriteBlittable(writer, values)) return;
#endif
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v);
        }

        private static void WriteStringArray(BinaryWriter writer, string[] values)
        {
            writer.Write(values.Length);
            //BinaryWriter cannot write a null string, so nulls travel as a sentinel
            foreach (var v in values) writer.Write(v ?? NULL_STRING);
        }

        private static void WriteTypeArray(BinaryWriter writer, Type[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v.ToConfigName());
        }

        private static void WriteGuidArray(BinaryWriter writer, Guid[] values)
        {
            writer.Write(values.Length);
#if NET8_0_OR_GREATER
            Span<byte> guidBuf = stackalloc byte[16];
            foreach (var v in values)
            {
                v.TryWriteBytes(guidBuf);
                writer.Write(guidBuf);
            }
#else
            foreach (var v in values) writer.Write(v.ToByteArray());
#endif
        }

        private static void WriteDateTimeArray(BinaryWriter writer, DateTime[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v.ToString("o"));
        }

        private static void WriteDateTimeBinaryArray(BinaryWriter writer, DateTime[] values)
        {
            writer.Write(values.Length);
            foreach (var v in values) writer.Write(v.ToBinary());
        }

        #endregion

        #region Receive

        public object[] ReceiveParameters(BinaryReader reader)
        {
            return ReceiveParameters(reader, WireVersion.V1);
        }

        internal object[] ReceiveParameters(BinaryReader reader, WireVersion version)
        {
            int parameterCount = reader.ReadInt32();
            object[] parameters = new object[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                parameters[i] = ReadParameter(reader, version);
            }
            return parameters;
        }

        private object ReadParameter(BinaryReader reader, WireVersion version)
        {
            //read type byte
            byte typeByte = reader.ReadByte();
            if (typeByte == ParameterTypes.Null) return null;

            //v2 codes are illegal in v1 streams and are rejected exactly like a code
            //this release does not know at all
            if (version == WireVersion.V1 && IsWireV2Only(typeByte)) throw UnknownTypeByte(typeByte);

            switch (typeByte)
            {
                case ParameterTypes.Bool: return reader.ReadBoolean();
                case ParameterTypes.Byte: return reader.ReadByte();
                case ParameterTypes.ByteArray: return reader.ReadBytes(reader.ReadInt32());
                case ParameterTypes.CompressedByteArray: return _compressor.DeCompress(reader.ReadBytes(reader.ReadInt32()));
                case ParameterTypes.Char: return reader.ReadChar();
                case ParameterTypes.CharArray: return reader.ReadChars(reader.ReadInt32());
                case ParameterTypes.CompressedCharArray: return Encoding.UTF8.GetChars(_compressor.DeCompress(reader.ReadBytes(reader.ReadInt32())));
                case ParameterTypes.Decimal: return reader.ReadDecimal();
                case ParameterTypes.Double: return reader.ReadDouble();
                case ParameterTypes.Float: return reader.ReadSingle();
                case ParameterTypes.Int: return reader.ReadInt32();
                case ParameterTypes.Long: return reader.ReadInt64();
                case ParameterTypes.SByte: return reader.ReadSByte();
                case ParameterTypes.Short: return reader.ReadInt16();
                case ParameterTypes.String: return reader.ReadString();
                case ParameterTypes.CompressedString: return Encoding.UTF8.GetString(_compressor.DeCompress(reader.ReadBytes(reader.ReadInt32())));
                case ParameterTypes.UInt: return reader.ReadUInt32();
                case ParameterTypes.ULong: return reader.ReadUInt64();
                case ParameterTypes.UShort: return reader.ReadUInt16();
                case ParameterTypes.Type: return reader.ReadString().ToType();
                case ParameterTypes.Guid: return new Guid(reader.ReadBytes(16));
                case ParameterTypes.DateTime: return ReadDateTimeText(reader);
                case ParameterTypes.DateTime2: return DateTime.FromBinary(reader.ReadInt64());

                case ParameterTypes.ArrayBool: return ReadBoolArray(reader);
                case ParameterTypes.ArraySByte: return ReadSByteArray(reader);
                case ParameterTypes.ArrayDecimal: return ReadDecimalArray(reader);
                case ParameterTypes.ArrayDouble: return ReadDoubleArray(reader);
                case ParameterTypes.ArrayFloat: return ReadFloatArray(reader);
                case ParameterTypes.ArrayInt: return ReadIntArray(reader);
                case ParameterTypes.ArrayUInt: return ReadUIntArray(reader);
                case ParameterTypes.ArrayLong: return ReadLongArray(reader);
                case ParameterTypes.ArrayULong: return ReadULongArray(reader);
                case ParameterTypes.ArrayShort: return ReadShortArray(reader);
                case ParameterTypes.ArrayUShort: return ReadUShortArray(reader);
                case ParameterTypes.ArrayString: return ReadStringArray(reader);
                case ParameterTypes.ArrayType: return ReadTypeArray(reader);
                case ParameterTypes.ArrayGuid: return ReadGuidArray(reader);
                case ParameterTypes.ArrayDateTime: return ReadDateTimeTextArray(reader);
                case ParameterTypes.ArrayDateTime2: return ReadDateTimeBinaryArray(reader);

                case ParameterTypes.Unknown: return ReadSerialized(reader, false);
                case ParameterTypes.CompressedUnknown: return ReadSerialized(reader, true);
                default:
                    throw UnknownTypeByte(typeByte);
            }
        }

        private static bool IsWireV2Only(byte typeByte)
        {
            return typeByte == ParameterTypes.DateTime2 || typeByte == ParameterTypes.ArrayDateTime2;
        }

        private object ReadSerialized(BinaryReader reader, bool compressed)
        {
            var typeConfigName = reader.ReadString();
            var bytes = reader.ReadBytes(reader.ReadInt32());
            if (compressed) bytes = _compressor.DeCompress(bytes);
            return _serializer.Deserialize(bytes, typeConfigName);
        }

        private static DateTime ReadDateTimeText(BinaryReader reader)
        {
            //InvariantCulture rather than the ambient culture: the writer always emits
            //the culture-independent "o" round-trip format, so the reader should not
            //depend on whatever culture the calling thread happens to carry. Passing
            //null parsed correctly in practice - RoundtripKind takes an ISO-8601 path
            //that ignores the culture's calendar - but relying on that was implicit.
            return DateTime.Parse(reader.ReadString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static bool[] ReadBoolArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new bool[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadBoolean();
            return values;
        }

        private static sbyte[] ReadSByteArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new sbyte[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadSByte();
            return values;
        }

        private static decimal[] ReadDecimalArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new decimal[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadDecimal();
            return values;
        }

        private static double[] ReadDoubleArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out double[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new double[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadDouble();
            return values;
        }

        private static float[] ReadFloatArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out float[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new float[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadSingle();
            return values;
        }

        private static int[] ReadIntArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out int[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new int[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadInt32();
            return values;
        }

        private static uint[] ReadUIntArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out uint[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new uint[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadUInt32();
            return values;
        }

        private static long[] ReadLongArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out long[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new long[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadInt64();
            return values;
        }

        private static ulong[] ReadULongArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out ulong[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new ulong[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadUInt64();
            return values;
        }

        private static short[] ReadShortArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out short[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new short[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadInt16();
            return values;
        }

        private static ushort[] ReadUShortArray(BinaryReader reader)
        {
#if NET8_0_OR_GREATER
            if (TryReadBlittable(reader, out ushort[] fast)) return fast;
#endif
            var len = reader.ReadInt32();
            var values = new ushort[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadUInt16();
            return values;
        }

        private static string[] ReadStringArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new string[len];
            for (int x = 0; x < len; x++)
            {
                var value = reader.ReadString();
                values[x] = value == NULL_STRING ? null : value;
            }
            return values;
        }

        private static Type[] ReadTypeArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new Type[len];
            for (int x = 0; x < len; x++) values[x] = reader.ReadString().ToType();
            return values;
        }

        private static Guid[] ReadGuidArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new Guid[len];
            for (int x = 0; x < len; x++) values[x] = new Guid(reader.ReadBytes(16));
            return values;
        }

        private static DateTime[] ReadDateTimeTextArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new DateTime[len];
            for (int x = 0; x < len; x++) values[x] = ReadDateTimeText(reader);
            return values;
        }

        private static DateTime[] ReadDateTimeBinaryArray(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            var values = new DateTime[len];
            for (int x = 0; x < len; x++) values[x] = DateTime.FromBinary(reader.ReadInt64());
            return values;
        }

        #endregion

        private static Exception UnknownTypeByte(byte typeByte)
        {
            return new Exception(string.Format("Unknown type byte '0x{0:X}'", typeByte));
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
