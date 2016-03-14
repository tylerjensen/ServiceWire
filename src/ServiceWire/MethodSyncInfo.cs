#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;

#endregion


namespace ServiceWire
{
    public class MethodSyncInfo
    {
        #region  Proporties

        public int MethodIdent { get; set; }
        public string MethodName { get; set; }
        public Type[] ParameterTypes { get; set; }

        #endregion
    }
}