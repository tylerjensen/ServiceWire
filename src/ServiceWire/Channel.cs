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
    public abstract class Channel:IDisposable
    {
        #region Fields

        protected Type _serviceType;

        #endregion


        #region Methods


        #region Protected Methods

        /// <summary>
        ///     Invokes the method with the specified parameters.
        /// </summary>
        /// <param name="parameters">Parameters for the method call</param>
        /// <returns>
        ///     An array of objects containing the return value (index 0) and the parameters used to call
        ///     the method, including any marked as "ref" or "out"
        /// </returns>
        protected abstract object[] InvokeMethod(string metaData,params object[] parameters);

        /// <summary>
        ///     Channel must implement an interface synchronization method.
        ///     This method asks the server for a list of identifiers paired with method
        ///     names and -parameter types. This is used when invoking methods server side.
        ///     When username and password supplied, zero knowledge encryption is used.
        /// </summary>
        protected abstract void SyncInterface(Type serviceType,string username=null,string password=null);

        #endregion


        #endregion


        #region IDisposable Members

        protected bool _disposed=false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        #endregion
    }
}