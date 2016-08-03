using System;

namespace ServiceWire.Aspects
{
    public class CrossCuttingConcerns
    {
        /// <summary>
        /// Takes instanceId, methodName and method parameters as object array.
        /// </summary>
        public Action<int, string, object[]> PreInvoke { get; set; }

        /// <summary>
        /// Takes instanceId, methodName, method parameters as object array, and exception thrown. Return true to call throw and raise exception.
        /// </summary>
        public Func<int, string, object[], Exception, bool> ExceptionHandler { get; set; }

        /// <summary>
        /// Takes instanceId, methodName and object array returned by execution of method.
        /// </summary>
        public Action<int, string, object[]> PostInvoke { get; set; }
    }
}