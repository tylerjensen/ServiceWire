/*
Copyright 2020 Tyler Jensen

Modified by Kenneth Carter

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Text.RegularExpressions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceWire
{
    public static class NetExtensions
    {
        public static string ToConfigName(this Type t)
        {
            // Do not qualify types from mscorlib/System.Private.CoreLib otherwise calling between process running with different frameworks won't work
            // i.e. "System.String, mscorlib" (.NET FW) != "System.String, System.Private.CoreLib" (.NET CORE)
            if (t.Assembly.GetName().Name == "mscorlib" ||
                t.Assembly.GetName().Name == "System.Private.CoreLib")
            {
                return t.FullName;
            }

            var name = t.AssemblyQualifiedName;
            name = Regex.Replace(name, @", Version=\d+.\d+.\d+.\d+", string.Empty);
            name = Regex.Replace(name, @", Culture=\w+", string.Empty);
            name = Regex.Replace(name, @", PublicKeyToken=\w+", string.Empty);
            return name;
        }

        public static Type ToType(this string configName)
        {
            try
            {
                var result = Type.GetType(configName);

                return result ?? TypeMapper.GetType(configName.Substring(0, configName.IndexOf(',')));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        public static async Task<Type> ToTypeAsync(this string configName)
        {
            try
            {
                var result = Type.GetType(configName);

                return result ?? await TypeMapper.GetTypeAsync(configName.Substring(0, configName.IndexOf(',')));
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