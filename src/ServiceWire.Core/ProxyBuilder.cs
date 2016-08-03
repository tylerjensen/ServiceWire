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
    }
}