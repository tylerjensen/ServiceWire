using System.Text.RegularExpressions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace ServiceWire
{
    public static class NetExtensions
    {
        static TypeCacheImpl _typeCache = new TypeCacheImpl();

        public static string ToConfigName(this Type t)
        {
            var aname = t.Assembly.GetName();
            // Do not qualify types from mscorlib/System.Private.CoreLib otherwise calling between process running with different frameworks won't work
            // i.e. "System.String, mscorlib" (.NET FW) != "System.String, System.Private.CoreLib" (.NET CORE)
            if (aname.Name == "mscorlib" ||
                aname.Name == "System.Private.CoreLib")
                return t.FullName;

            // confgiName is cached for non system types (Regex.Replace is not executed on each serialization)
            var cached = _typeCache[t];
            if (cached == null)
            {
                var name = t.AssemblyQualifiedName;
                var shortName = Regex.Replace(name, @", Version=\d+.\d+.\d+.\d+", string.Empty);
                shortName = Regex.Replace(shortName, @", Culture=\w+", string.Empty);
                shortName = Regex.Replace(shortName, @", PublicKeyToken=\w+", string.Empty);

                var test = ToType(shortName); // short name is checked to be loaded
                if (test != null)
                    name = shortName;
                
                cached = new TypeCacheEntry { ConfigName = name };
                _typeCache.Add(t, cached);
            }
            return cached.ConfigName;
        }

        public static Type ToType(this string configName)
        {
            try
            {
                var result = Type.GetType(configName);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
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