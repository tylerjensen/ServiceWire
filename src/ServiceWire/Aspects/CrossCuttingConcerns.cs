#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:35

#endregion


#region Usings

using System;

#endregion


namespace ServiceWire.Aspects
{
    public class CrossCuttingConcerns
    {
        #region  Proporties

        /// <summary>
        ///     Takes instanceId, methodName and method parameters as object array.
        /// </summary>
        public Action<int,string,object[]> PreInvoke { get; set; }

        /// <summary>
        ///     Takes instanceId, methodName, method parameters as object array, and exception thrown. Return true to call throw
        ///     and raise exception.
        /// </summary>
        public Func<int,string,object[],Exception,bool> ExceptionHandler { get; set; }

        /// <summary>
        ///     Takes instanceId, methodName and object array returned by execution of method.
        /// </summary>
        public Action<int,string,object[]> PostInvoke { get; set; }

        #endregion
    }
}