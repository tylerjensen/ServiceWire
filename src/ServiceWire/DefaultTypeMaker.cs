#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:35

#endregion


#region Usings

using System;

#endregion


namespace ServiceWire
{
    internal class DefaultTypeMaker
    {
        #region Methods


        #region Public Methods

        public object GetDefault(Type t)
        {
            return GetType().GetMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(this,null);
        }

        public T GetDefaultGeneric<T>()
        {
            return default(T);
        }

        #endregion


        #endregion
    }
}