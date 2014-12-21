using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
#if (NET35)
using ServiceWire.SvcStkTxt;
#endif
using System.Text;
using ServiceWire.SvcStkTxt;

namespace ServiceWire
{
    public static class NetExtensions
    {
        public static string ToConfigName(this Type t)
        {
            return t.FullName + ", " + t.Assembly.GetName().Name;
        }

        public static Type ToType(this string configName)
        {
            try
            {
                var parts = (from n in configName.Split(',') select n.Trim()).ToArray();
                var assembly = Assembly.Load(new AssemblyName(parts[1]));
                var type = assembly.GetType(parts[0]);
                return type;
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static byte[] ToSerializedBytes<T>(this T obj)
        {
            if (null == obj) return null;
            return Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(obj));
        }

        public static T ToDeserializedObject<T>(this byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            return TypeSerializer.DeserializeFromString<T>(Encoding.UTF8.GetString(bytes));
        }

        public static object ToDeserializedObject(this byte[] bytes, string typeFullName)
        {
            if (null == typeFullName || null == bytes || bytes.Length == 0) return null;
            var type = Type.GetType(typeFullName);
            return TypeSerializer.DeserializeFromString(Encoding.UTF8.GetString(bytes), type);
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