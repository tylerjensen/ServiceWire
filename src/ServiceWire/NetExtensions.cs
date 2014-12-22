using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceWire
{
    public static class NetExtensions
    {
        public static string ToConfigName(this Type t)
        {
            var name = t.AssemblyQualifiedName;
            name = Regex.Replace(name, @", Version=\d+.\d+.\d+.\d+", string.Empty);
            name = Regex.Replace(name, @", Culture=\w+", string.Empty);
            name = Regex.Replace(name, @", PublicKeyToken=\w+", string.Empty);
            return name;
            //return t.FullName + ", " + t.Assembly.GetName().Name;
        }

        public static Type ToType(this string configName)
        {
            try
            {
                return Type.GetType(configName);
                //var parts = (from n in configName.Split(',') select n.Trim()).ToArray();
                //var assembly = Assembly.Load(new AssemblyName(parts[1]));
                //var type = assembly.GetType(parts[0]);
                //return type;
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };

        public static byte[] ToSerializedBytes<T>(this T obj)
        {
            if (null == obj) return null;
            var json = JsonConvert.SerializeObject(obj, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public static byte[] ToSerializedBytes(this object obj, string typeConfigName)
        {
            if (null == obj) return null;
            var type = typeConfigName.ToType();
            var json = JsonConvert.SerializeObject(obj, type, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T ToDeserializedObject<T>(this byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), settings);
        }

        public static object ToDeserializedObject(this byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName || null == bytes || bytes.Length == 0) return null;
            var type = typeConfigName.ToType();
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type, settings);
        }

        public static string ToSerializedBase64String<T>(this T obj)
        {
            if (null == obj) return null;
            var bytes = obj.ToSerializedBytes();
            return Convert.ToBase64String(bytes);
        }

        public static T ToDeserializedObjectFromBase64String<T>(this string base64String)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64String);
                return bytes.ToDeserializedObject<T>();
            }
            catch { }
            return default(T);
        }

        /// <summary>
        /// Returns true if Type inherits from baseType.
        /// </summary>
        /// <param name="t">The Type extended by this method.</param>
        /// <param name="baseType">The base type to find in the inheritance hierarchy.</param>
        /// <returns>True if baseType is found. False if not.</returns>
        public static bool InheritsFrom(this Type t, Type baseType)
        {
            Type cur = t.BaseType;
            while (cur != null)
            {
                if (cur.Equals(baseType)) return true;
                cur = cur.BaseType;
            }
            return false;
        }

        public static object GetDefault(this Type t)
        {
            var tm = new DefaultTypeMaker();
            return tm.GetDefault(t);
        }

        public static byte[] ToGZipBytes(this byte[] data)
        {
            using (var msCompressed = new MemoryStream())
            {
                using (var msObj = new MemoryStream(data))
                {
                    using (GZipStream gzs = new GZipStream(msCompressed, CompressionMode.Compress))
                    {
                        msObj.CopyTo(gzs);
                    }
                }
                return msCompressed.ToArray();
            }
        }

#if (NET35)
        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
#endif

        public static byte[] FromGZipBytes(this byte[] compressedBytes)
        {
            using (var msObj = new MemoryStream())
            {
                using (var msCompressed = new MemoryStream(compressedBytes))
                using (var gzs = new GZipStream(msCompressed, CompressionMode.Decompress))
                {
                    gzs.CopyTo(msObj);
                }
                msObj.Seek(0, SeekOrigin.Begin);
                return msObj.ToArray();
            }
        }

        public static string Flatten(this string src)
        {
            return src.Replace("\r", ":").Replace("\n", ":");
        }
    }
}