using System;
using System.Reflection;

namespace ServiceWire
{
    internal class DefaultTypeMaker
    {
        public object GetDefault(Type t)
        {
#if !NETSTANDARD1_6
            return this.GetType().GetMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(this, null);
#else
            return this.GetType().GetTypeInfo().GetMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(this, null);
#endif
        }

        public T GetDefaultGeneric<T>()
        {
            return default(T);
        }
    }
}