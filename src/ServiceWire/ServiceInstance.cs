#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Collections.Concurrent;
using System.Reflection;

#endregion


namespace ServiceWire
{
    public class ServiceInstance
    {
        #region  Proporties

        public int KeyIndex { get; set; }
        public Type InterfaceType { get; set; }
        public object SingletonInstance { get; set; }
        public ConcurrentDictionary<int,MethodInfo> InterfaceMethods { get; set; }
        public ConcurrentDictionary<int,bool[]> MethodParametersByRef { get; set; }
        public ServiceSyncInfo ServiceSyncInfo { get; set; }

        #endregion
    }
}