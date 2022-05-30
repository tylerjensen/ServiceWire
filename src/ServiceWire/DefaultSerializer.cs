using System;
using System.IO;
using System.Text.Json;

namespace ServiceWire
{
    public class DefaultSerializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            if (null == obj) return null;
            return JsonSerializer.SerializeToUtf8Bytes<T>(obj);
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            if (null == obj) return null;
            return JsonSerializer.SerializeToUtf8Bytes(obj, typeConfigName.ToType());
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            return JsonSerializer.Deserialize<T>(bytes);
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName) throw new ArgumentNullException(nameof(typeConfigName));
            var type = typeConfigName.ToType();
            if (null == typeConfigName || null == bytes || bytes.Length == 0) return type.GetDefault();
            return JsonSerializer.Deserialize(bytes, typeConfigName.ToType());
        }
    }
}
