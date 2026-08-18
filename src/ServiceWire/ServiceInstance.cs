using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ServiceWire
{
    public class ServiceInstance
    {
        public int KeyIndex { get; set; }
        public Type InterfaceType { get; set; }
        public object SingletonInstance { get; set; }
        public ConcurrentDictionary<int, MethodInfo> InterfaceMethods { get; set; }
        public ConcurrentDictionary<int, bool[]> MethodParametersByRef { get; set; }
        public ServiceSyncInfo ServiceSyncInfo { get; set; }

        /// <summary>
        /// Compiled invokers per method ident. Methods with byref parameters are
        /// absent and fall back to MethodInfo.Invoke.
        /// </summary>
        internal ConcurrentDictionary<int, Func<object, object[], object>> CompiledMethods { get; set; }
    }
}