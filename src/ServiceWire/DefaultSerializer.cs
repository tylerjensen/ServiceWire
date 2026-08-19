using System;
using System.IO;
using System.Text.Json;

namespace ServiceWire
{
    public class DefaultSerializer : ISerializer
    {
        //cached options: parameterless JsonSerializer overloads build default options anyway,
        //and the exception converter makes thrown exceptions serializable (see its remarks)
        private static readonly JsonSerializerOptions _options = CreateOptions();

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ExceptionJsonConverter());
            return options;
        }

        public byte[] Serialize<T>(T obj)
        {
            if (null == obj) return null;
            return JsonSerializer.SerializeToUtf8Bytes<T>(obj, _options);
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            if (null == obj) return null;
            return JsonSerializer.SerializeToUtf8Bytes(obj, typeConfigName.ToType(), _options);
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            return JsonSerializer.Deserialize<T>(bytes, _options);
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName) throw new ArgumentNullException(nameof(typeConfigName));
            var type = typeConfigName.ToType();
            if (null == bytes || bytes.Length == 0) return type.GetDefault();
            return JsonSerializer.Deserialize(bytes, type, _options);
        }
    }
}
