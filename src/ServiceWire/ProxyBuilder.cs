using System;
using System.Reflection.Emit;

namespace ServiceWire
{
    internal sealed class ProxyBuilder
    {
        public string ProxyName { get; set; }
        public Type InterfaceType { get; set; }
        public Type CtorType { get; set; }
        public AssemblyBuilder AssemblyBuilder { get; set; }
        public ModuleBuilder ModuleBuilder { get; set; }
        public TypeBuilder TypeBuilder { get; set; }

        /// <summary>The created proxy type; built once when the builder is created.</summary>
        public Type ProxyType { get; set; }

        /// <summary>Compiled constructor: (serviceType, ctorArg, serializer, compressor, logger, stats) => proxy instance.</summary>
        public Func<Type, object, ISerializer, ICompressor, ILog, IStats, object> CtorInvoker { get; set; }
    }
}