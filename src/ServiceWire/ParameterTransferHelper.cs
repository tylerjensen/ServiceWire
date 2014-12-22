using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceWire
{
    public sealed class ParameterTransferHelper
    {
        [ThreadStatic]
        private static Dictionary<Type, byte> _parameterTypes;

        public void SendParameters(bool useCompression, int compressionThreshold, BinaryWriter writer, params object[] parameters)
        {
            //write how many parameters are coming
            writer.Write(parameters.Length);
            //write data for each parameter
            foreach (object parameter in parameters)
            {
                if (parameter == null)
                    writer.Write(ParameterTypes.Null);
                else
                {
                    Type type = parameter.GetType();
                    byte typeByte = GetParameterType(type);
                    //determine whether to compress parameter
                    byte[] dataBytes = new byte[0];
                    //check for compressable values and compress if required
                    switch (typeByte)
                    {
                        case ParameterTypes.ByteArray:
                            dataBytes = (byte[])parameter;
                            if (useCompression && dataBytes.LongLength > compressionThreshold)
                            {
                                typeByte = ParameterTypes.CompressedByteArray;
                                dataBytes = dataBytes.ToGZipBytes();
                            }
                            break;
                        case ParameterTypes.CharArray:
                            char[] charArray = (char[])parameter;
                            if (useCompression && charArray.LongLength > compressionThreshold)
                            {
                                typeByte = ParameterTypes.CompressedCharArray;
                                dataBytes = Encoding.UTF8.GetBytes(charArray).ToGZipBytes();
                            }
                            break;
                        case ParameterTypes.String:
                            if (useCompression && ((string)parameter).Length > compressionThreshold)
                            {
                                typeByte = ParameterTypes.CompressedString;
                                dataBytes = Encoding.UTF8.GetBytes(((string)parameter)).ToGZipBytes();
                            }
                            break;
                        case ParameterTypes.ArrayString:
                            if (useCompression)
                            {
                                var array = (string[])parameter;
                                var total = (from n in array select n.Length).Sum();
                                if (total > compressionThreshold)
                                {
                                    typeByte = ParameterTypes.Unknown;
                                    dataBytes = array.ToSerializedBytes(type.ToConfigName()).ToGZipBytes();
                                }
                            }
                            break;
                        case ParameterTypes.Unknown:
                            dataBytes = parameter.ToSerializedBytes(type.ToConfigName());
                            if (useCompression && dataBytes.Length > compressionThreshold)
                            {
                                typeByte = ParameterTypes.CompressedUnknown;
                                dataBytes = dataBytes.ToGZipBytes();
                            }
                            break;
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
                            writer.Write(type.FullName);
                            break;
                        case ParameterTypes.Guid:
                            writer.Write(((Guid)parameter).ToByteArray());
                            break;
                        case ParameterTypes.DateTime:
                            writer.Write((string)((DateTime)parameter).ToString("o"));
                            break;

                        case ParameterTypes.ArrayBool:
                            var bools = (bool[])parameter;
                            writer.Write(bools.Length);
                            foreach(var b in bools) writer.Write(b);
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
                            writer.Write(dbls.Length);
                            foreach (var db in dbls) writer.Write(db);
                            break;  
                        case ParameterTypes.ArrayFloat:
                            var fls = (float[])parameter;
                            writer.Write(fls.Length);
                            foreach(var f in fls) writer.Write(f);
                            break;   
                        case ParameterTypes.ArrayInt:
                            var ints = (int[])parameter;
                            writer.Write(ints.Length);
                            foreach (var i in ints) writer.Write(i);
                            break;     
                        case ParameterTypes.ArrayUInt:
                            var uints = (uint[])parameter;
                            writer.Write(uints.Length);
                            foreach (var u in uints) writer.Write(u);
                            break;    
                        case ParameterTypes.ArrayLong:
                            var longs = (long[])parameter;
                            writer.Write(longs.Length);
                            foreach (var lg in longs) writer.Write(lg);
                            break;    
                        case ParameterTypes.ArrayULong:
                            var ulongs = (ulong[])parameter;
                            writer.Write(ulongs.Length);
                            foreach (var ul in ulongs) writer.Write(ul);
                            break;   
                        case ParameterTypes.ArrayShort:
                            var shorts = (short[])parameter;
                            writer.Write(shorts.Length);
                            foreach (var s in shorts) writer.Write(s);
                            break;   
                        case ParameterTypes.ArrayUShort:
                            var ushorts = (ushort[])parameter;
                            writer.Write(ushorts.Length);
                            foreach (var us in ushorts) writer.Write(us);
                            break;  
                        case ParameterTypes.ArrayString:
                            var strings = (string[])parameter;
                            writer.Write(strings.Length);
                            foreach (var st in strings) writer.Write(st);
                            break;  
                        case ParameterTypes.ArrayType:
                            var types = (Type[])parameter;
                            writer.Write(types.Length);
                            foreach (var t in types)
                                writer.Write(t.FullName);
                            break;    
                        case ParameterTypes.ArrayGuid:
                            var guids = (Guid[])parameter;
                            writer.Write(guids.Length);
                            foreach (var g in guids) writer.Write(g.ToByteArray());
                            break;
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
                    parameters[i] = null;
                else
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
                            parameters[i] = reader.ReadBytes(reader.ReadInt32()).FromGZipBytes();
                            break;
                        case ParameterTypes.Char:
                            parameters[i] = reader.ReadChar();
                            break;
                        case ParameterTypes.CharArray:
                            parameters[i] = reader.ReadChars(reader.ReadInt32());
                            break;
                        case ParameterTypes.CompressedCharArray:
                            var ccBytes = reader.ReadBytes(reader.ReadInt32()).FromGZipBytes();
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
                            var csBytes = reader.ReadBytes(reader.ReadInt32()).FromGZipBytes();
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
                            parameters[i] = Type.GetType(typeName);
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
                            var dblen = reader.ReadInt32();
                            var dbs = new double[dblen];
                            for (int x = 0; x < dblen; x++) dbs[x] = reader.ReadDouble();
                            parameters[i] = dbs;
                            break;
                        case ParameterTypes.ArrayFloat:
                            var flen = reader.ReadInt32();
                            var fs = new float[flen];
                            for (int x = 0; x < flen; x++) fs[x] = reader.ReadSingle();
                            parameters[i] = fs;
                            break;
                        case ParameterTypes.ArrayInt:
                            var ilen = reader.ReadInt32();
                            var iss = new int[ilen];
                            for (int x = 0; x < ilen; x++) iss[x] = reader.ReadInt32();
                            parameters[i] = iss;
                            break;
                        case ParameterTypes.ArrayUInt:
                            var uilen = reader.ReadInt32();
                            var uis = new uint[uilen];
                            for (int x = 0; x < uilen; x++) uis[x] = reader.ReadUInt32();
                            parameters[i] = uis;
                            break;
                        case ParameterTypes.ArrayLong:
                            var llen = reader.ReadInt32();
                            var ls = new long[llen];
                            for (int x = 0; x < llen; x++) ls[x] = reader.ReadInt64();
                            parameters[i] = ls;
                            break;
                        case ParameterTypes.ArrayULong:
                            var ullen = reader.ReadInt32();
                            var uls = new ulong[ullen];
                            for (int x = 0; x < ullen; x++) uls[x] = reader.ReadUInt64();
                            parameters[i] = uls;
                            break;
                        case ParameterTypes.ArrayShort:
                            var sslen = reader.ReadInt32();
                            var sss = new short[sslen];
                            for (int x = 0; x < sslen; x++) sss[x] = reader.ReadInt16();
                            parameters[i] = sss;
                            break;
                        case ParameterTypes.ArrayUShort:
                            var ulen = reader.ReadInt32();
                            var us = new ushort[ulen];
                            for (int x = 0; x < ulen; x++) us[x] = reader.ReadUInt16();
                            parameters[i] = us;
                            break;
                        case ParameterTypes.ArrayString:
                            var slen = reader.ReadInt32();
                            var ss = new string[slen];
                            for (int x = 0; x < slen; x++) ss[x] = reader.ReadString();
                            parameters[i] = ss;
                            break;
                        case ParameterTypes.ArrayType:
                            var tlen = reader.ReadInt32();
                            var ts = new Type[tlen];
                            for (int x = 0; x < tlen; x++) ts[x] = Type.GetType(reader.ReadString());
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
                            parameters[i] = bytes.ToDeserializedObject(typeConfigName);
                            break;
                        case ParameterTypes.CompressedUnknown:
                            var cuTypeConfigName = reader.ReadString();
                            var cuBytes = reader.ReadBytes(reader.ReadInt32()).FromGZipBytes();
                            parameters[i] = cuBytes.ToDeserializedObject(cuTypeConfigName);
                            break;
                        default:
                            throw new Exception(string.Format("Unknown type byte '0x{0:X}'", typeByte));
                    }
                }
            }
            return parameters;
        }

        private byte GetParameterType(Type type)
        {
            InitializeParamTypes();
            if (_parameterTypes.ContainsKey(type))
                return _parameterTypes[type];
            return ParameterTypes.Unknown;
        }

        private void InitializeParamTypes()
        {
            if (_parameterTypes == null)
            {
                _parameterTypes = new Dictionary<Type, byte>();
                _parameterTypes.Add(typeof(bool), ParameterTypes.Bool);
                _parameterTypes.Add(typeof(byte), ParameterTypes.Byte);
                _parameterTypes.Add(typeof(sbyte), ParameterTypes.SByte);
                _parameterTypes.Add(typeof(char), ParameterTypes.Char);
                _parameterTypes.Add(typeof(decimal), ParameterTypes.Decimal);
                _parameterTypes.Add(typeof(double), ParameterTypes.Double);
                _parameterTypes.Add(typeof(float), ParameterTypes.Float);
                _parameterTypes.Add(typeof(int), ParameterTypes.Int);
                _parameterTypes.Add(typeof(uint), ParameterTypes.UInt);
                _parameterTypes.Add(typeof(long), ParameterTypes.Long);
                _parameterTypes.Add(typeof(ulong), ParameterTypes.ULong);
                _parameterTypes.Add(typeof(short), ParameterTypes.Short);
                _parameterTypes.Add(typeof(ushort), ParameterTypes.UShort);
                _parameterTypes.Add(typeof(string), ParameterTypes.String);
                _parameterTypes.Add(typeof(byte[]), ParameterTypes.ByteArray);
                _parameterTypes.Add(typeof(char[]), ParameterTypes.CharArray);
                _parameterTypes.Add(typeof(Type), ParameterTypes.Type);
                _parameterTypes.Add(typeof(Guid), ParameterTypes.Guid);
                _parameterTypes.Add(typeof(DateTime), ParameterTypes.DateTime);

                _parameterTypes.Add(typeof(bool[]), ParameterTypes.ArrayBool);
                _parameterTypes.Add(typeof(sbyte[]), ParameterTypes.ArraySByte);
                _parameterTypes.Add(typeof(decimal[]), ParameterTypes.ArrayDecimal);
                _parameterTypes.Add(typeof(double[]), ParameterTypes.ArrayDouble);
                _parameterTypes.Add(typeof(float[]), ParameterTypes.ArrayFloat);
                _parameterTypes.Add(typeof(int[]), ParameterTypes.ArrayInt);
                _parameterTypes.Add(typeof(uint[]), ParameterTypes.ArrayUInt);
                _parameterTypes.Add(typeof(long[]), ParameterTypes.ArrayLong);
                _parameterTypes.Add(typeof(ulong[]), ParameterTypes.ArrayULong);
                _parameterTypes.Add(typeof(short[]), ParameterTypes.ArrayShort);
                _parameterTypes.Add(typeof(ushort[]), ParameterTypes.ArrayUShort);
                _parameterTypes.Add(typeof(string[]), ParameterTypes.ArrayString);
                _parameterTypes.Add(typeof(Type[]), ParameterTypes.ArrayType);
                _parameterTypes.Add(typeof(Guid[]), ParameterTypes.ArrayGuid);
                _parameterTypes.Add(typeof(DateTime[]), ParameterTypes.ArrayDateTime);
            }
        }
    }
}
