using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ServiceWire
{
    public static class NetExtensions
    {
        private const string _netFwCoreLib = "mscorlib";
        private const string _netCoreCoreLib = "System.Private.CoreLib";

        //config names are pure functions of the type and vice versa, and the population is
        //bounded by the contract types in play, so both directions are memoized for the life
        //of the process to keep regex and Type.GetType costs off the per-call wire path
        private static readonly ConcurrentDictionary<Type, string> _configNameCache = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

        public static string ToConfigName(this Type t)
        {
            return _configNameCache.GetOrAdd(t, BuildConfigName);
        }

        private static string BuildConfigName(Type t)
        {
            // Do not qualify types from mscorlib/System.Private.CoreLib otherwise calling between process running with different frameworks won't work
            // i.e. "System.String, mscorlib" (.NET FW) != "System.String, System.Private.CoreLib" (.NET CORE)
            var name = t.Assembly.GetName().Name == _netFwCoreLib || t.Assembly.GetName().Name == _netCoreCoreLib
                ? t.FullName
                : t.AssemblyQualifiedName;

            // But since an mscorlib generic container can contain fully qualified types we always need to clean up the name
            name = Regex.Replace(name, @", Version=\d+.\d+.\d+.\d+", string.Empty);
            name = Regex.Replace(name, @", Culture=\w+", string.Empty);
            name = Regex.Replace(name, @", PublicKeyToken=\w+", string.Empty);
            name = name.Replace(", " + _netFwCoreLib, string.Empty);
            name = name.Replace(", " + _netCoreCoreLib, string.Empty);

            return name;
        }

        public static Type ToType(this string configName)
        {
            //only successful lookups are cached: a null result may succeed later once
            //the assembly holding the type has been loaded
            if (_typeCache.TryGetValue(configName, out var cached)) return cached;
            try
            {
                var result = Type.GetType(configName);
                if (null != result) _typeCache.TryAdd(configName, result);
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