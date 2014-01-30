using System;
using System.Collections.Concurrent;
using System.IO;

namespace ServiceWire
{
    public class StreamingChannel : Channel, IDvChannel
    {
        private object _syncRoot = new object();
        protected BinaryReader _binReader;
        protected BinaryWriter _binWriter;
        protected Stream _stream;
        private ParameterTransferHelper _parameterTransferHelper = new ParameterTransferHelper();
        private ServiceSyncInfo _syncInfo;

        // keep cached sync info to avoid redundant wire trips
        private static ConcurrentDictionary<Type, ServiceSyncInfo> _syncInfoCache = new ConcurrentDictionary<Type, ServiceSyncInfo>(); 

        /// <summary>
        /// Returns true if client is connected to the server.
        /// </summary>
        public virtual bool IsConnected { get { return false; } }

        /// <summary>
        /// This method asks the server for a list of identifiers paired with method
        /// names and -parameter types. This is used when invoking methods server side.
        /// </summary>
        protected override void SyncInterface(Type serviceType)
        {
            if (!_syncInfoCache.TryGetValue(serviceType, out _syncInfo))
            {
                //write the message type
                _binWriter.Write((int)MessageType.SyncInterface);
                _binWriter.Write(serviceType.AssemblyQualifiedName ?? serviceType.FullName);

                //read sync data
                var bytes = _binReader.ReadBytes(_binReader.ReadInt32());
                _syncInfo = (ServiceSyncInfo)bytes.ToDeserializedObject();

                _syncInfoCache.AddOrUpdate(serviceType, _syncInfo, (t, info) => _syncInfo);
            }
        }

        /// <summary>
        /// Invokes the method with the specified parameters.
        /// </summary>
        /// <param name="metaData">Method name and parameter type names.</param>
        /// <param name="parameters">Parameters for the method call</param>
        /// <returns>An array of objects containing the return value (index 0) and the parameters used to call
        /// the method, including any marked as "ref" or "out"</returns>
        protected override object[] InvokeMethod(string metaData, params object[] parameters)
        {
            //prevent call to invoke method on more than one thread at a time
            lock (_syncRoot)
            {
                var mdata = metaData.Split('|');

                //write the message type
                _binWriter.Write((int)MessageType.MethodInvocation);

                //find the matching server side method ident
                var ident = -1;
                for (int index = 0; index < _syncInfo.MethodInfos.Length; index++)
                {
                    var si = _syncInfo.MethodInfos[index];
                    //first of all the method names must match
                    if (si.MethodName == mdata[0])
                    {
                        //second of all the parameter types and -count must match
                        if (mdata.Length - 1 == si.ParameterTypes.Length)
                        {
                            var matchingParameterTypes = true;
                            for (int i = 0; i < si.ParameterTypes.Length; i++)
                                if (!mdata[i + 1].Equals(si.ParameterTypes[i].FullName))
                                {
                                    matchingParameterTypes = false;
                                    break;
                                }
                            if (matchingParameterTypes)
                            {
                                ident = si.MethodIdent;
                                break;
                            }
                        }
                    }
                }

                if (ident < 0)
                    throw new Exception(string.Format("Cannot match method '{0}' to its server side equivalent", mdata[0]));

                //write service key index
                _binWriter.Write(_syncInfo.ServiceKeyIndex);

                //write the method ident to the server
                _binWriter.Write(ident);

                //send the parameters
                _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                    _syncInfo.CompressionThreshold,
                    _binWriter,
                    parameters);

                _binWriter.Flush();
                _stream.Flush();

                // Read the result of the invocation.
                MessageType messageType = (MessageType)_binReader.ReadInt32();
                if (messageType == MessageType.UnknownMethod)
                    throw new Exception("Unknown method.");

                object[] outParams = _parameterTransferHelper.ReceiveParameters(_binReader);

                if (messageType == MessageType.ThrowException)
                    throw (Exception)outParams[0];

                return outParams;
            }
        }

        #region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    try
                    {
                        _binWriter.Write((int)MessageType.TerminateConnection);
                        //_binWriter.Flush();
                        //_stream.Flush();
                    }
                    finally
                    {
                        if (null != _binWriter) _binWriter.Dispose();
                        if (null != _binReader) _binReader.Dispose();
                        //if (null != _stream) _stream.Dispose();
                    }
                }
            }
        }

        #endregion
    }
}
