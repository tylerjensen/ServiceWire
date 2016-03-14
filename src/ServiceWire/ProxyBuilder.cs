#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Reflection.Emit;

#endregion


namespace ServiceWire
{
    internal sealed class ProxyBuilder
    {
        #region  Proporties

        public string ProxyName { get; set; }
        public Type InterfaceType { get; set; }
        public Type CtorType { get; set; }
        public AssemblyBuilder AssemblyBuilder { get; set; }
        public ModuleBuilder ModuleBuilder { get; set; }
        public TypeBuilder TypeBuilder { get; set; }

        #endregion
    }
}