using System;
using System.Text.RegularExpressions;

namespace ServiceWire
{
    public static class NetExtensions
    {
        public static string ToConfigName(this Type t)
        {
            // Do not qualify types from mscorlib/System.Private.CoreLib otherwise calling between process running with different frameworks won't work
            // i.e. "System.String, mscorlib" (.NET FW) != "System.String, System.Private.CoreLib" (.NET CORE)
            var name = ((t.Assembly.GetName().Name == "mscorlib" || t.Assembly.GetName().Name == "System.Private.CoreLib")) ? t.FullName : t.AssemblyQualifiedName;
            // But since an mscorlib generic container can contain fully qualified types we always need to clean up the name
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

        public static string Flatten(this string src)
        {
            return src.Replace("\r", ":").Replace("\n", ":");
        }
    }
}