using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using ServiceWire;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ServiceWire.Benchmarks
{
    public class ProtobufSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] bytes)
        {
            if (bytes.Length == 0) return default(T);
            using (var ms = new MemoryStream(bytes))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName) throw new ArgumentNullException(nameof(typeConfigName));
            var type = typeConfigName.ToType();
            if (null == typeConfigName || null == bytes || bytes.Length == 0) return type.GetDefault();
            using (var ms = new MemoryStream(bytes))
            {
                return Serializer.Deserialize(type, ms);
            }
        }

        public byte[] Serialize<T>(T obj)
        {
            if (null == obj) return null;
            using (var ms = new MemoryStream())
            {
                try
                {
                    Serializer.Serialize<T>(ms, obj);
                    var bytes = ms.ToArray();
                    return bytes;
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
                return null;
            }
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            if (null == typeConfigName) throw new ArgumentNullException(nameof(typeConfigName));
            if (null == obj) return null;
            using (var ms = new MemoryStream())
            {
                try
                {
                    Serializer.Serialize(ms, obj);
                    var bytes = ms.ToArray();
                    return bytes;
                }
                catch (Exception e)
                {
                    Console.Write(e);
                }
                return null;
            }
        }
    }
}
