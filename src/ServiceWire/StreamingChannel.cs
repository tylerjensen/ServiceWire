using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Authentication;
using ServiceWire.ZeroKnowledge;

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
        private ZkCrypto _zkCrypto = null;

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
        protected override void SyncInterface(Type serviceType, 
            string username = null, string password = null)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                //do zk protocol authentication
                var sr = new ZkProtocol();

                // Step 1. Client sends username and ephemeral hash of random number.
                var aRand = sr.CryptRand();
                var aClientEphemeral = sr.GetClientEphemeralA(aRand);
                
                // send username and aClientEphemeral to server
                _binWriter.Write((int)MessageType.ZkInitiate);
                _binWriter.Write(username);
                _binWriter.Write(aClientEphemeral); //always 32 bytes
                
                // get response from server
                var userFound = _binReader.ReadBoolean();
                if (!userFound)
                {
                    throw new InvalidCredentialException("authentication failed");
                }
                var salt = _binReader.ReadBytes(32);
                var bServerEphemeral = _binReader.ReadBytes(32);

                // Step 3. Client and server calculate random scramble of ephemeral hash values exchanged.
                var clientScramble = sr.CalculateRandomScramble(aClientEphemeral, bServerEphemeral);

                // Step 4. Client computes session key
                var clientSessionKey = sr.ClientComputeSessionKey(salt, username, password, 
                    aClientEphemeral, bServerEphemeral, clientScramble);

                // Step 6. Client creates hash of session key and sends to server. Server creates same key and verifies.
                var clientSessionHash = sr.ClientCreateSessionHash(username, salt, aClientEphemeral,
                    bServerEphemeral, clientSessionKey);
                // send to server and server verifies
                _binWriter.Write((int)MessageType.ZkProof);
                _binWriter.Write(clientSessionHash); //always 32 bytes

                // get response
                var serverVerified = _binReader.ReadBoolean();
                if (!serverVerified)
                {
                    throw new InvalidCredentialException("authentication failed");
                }
                var serverSessionHash = _binReader.ReadBytes(32);
                var clientServerSessionHash = sr.ServerCreateSessionHash(aClientEphemeral, 
                    clientSessionHash, clientSessionKey);
                if (!serverSessionHash.IsEqualTo(clientServerSessionHash))
                {
                    throw new InvalidCredentialException("authentication failed");
                }
                _zkCrypto = new ZkCrypto(clientSessionKey, clientScramble);
            }
            
            if (!_syncInfoCache.TryGetValue(serviceType, out _syncInfo))
            {
                //write the message type
                _binWriter.Write((int)MessageType.SyncInterface);
                if (null != _zkCrypto)
                {
                    //sync interface with encryption
                    var assemName = serviceType.AssemblyQualifiedName ?? serviceType.FullName;
                    var assemblyNameEncrypted = _zkCrypto.Encrypt(assemName.ConvertToBytes());
                    _binWriter.Write(assemblyNameEncrypted.Length);
                    _binWriter.Write(assemblyNameEncrypted);
                }
                else
                {
                    _binWriter.Write(serviceType.AssemblyQualifiedName ?? serviceType.FullName);
                }
                //read sync data
                var len = _binReader.ReadInt32();
                //len is zero when AssemblyQualifiedName not same version or not found
                if (len == 0) throw new TypeAccessException("SyncInterface failed. Type or version of type unknown.");
                var bytes = _binReader.ReadBytes(len);
                if (null != _zkCrypto)
                {
                    bytes = _zkCrypto.Decrypt(bytes);
                }
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
                var useCrypto = null != _zkCrypto;
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

                //if encrypted, wrap up key index and params and send len then enc bytes
                if (useCrypto)
                {
                    byte[] callData;
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        //send the parameters
                        _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                            _syncInfo.CompressionThreshold,
                            bw,
                            parameters);
                        callData = ms.ToArray();
                    }
                    var encData = _zkCrypto.Encrypt(callData);
                    _binWriter.Write(encData.Length);
                    _binWriter.Write(encData);
                }
                else
                {
                    //send the parameters
                    _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                        _syncInfo.CompressionThreshold,
                        _binWriter,
                        parameters);
                }

                _binWriter.Flush();
                _stream.Flush();

                // Read the result of the invocation.
                MessageType messageType = (MessageType)_binReader.ReadInt32();
                if (messageType == MessageType.UnknownMethod)
                    throw new Exception("Unknown method.");

                object[] outParams;
                if (useCrypto)
                {
                    var len = _binReader.ReadInt32();
                    var encData = _binReader.ReadBytes(len);
                    var data = _zkCrypto.Decrypt(encData);
                    using (var ms = new MemoryStream(data))
                    using (var br = new BinaryReader(ms))
                    {
                        outParams = _parameterTransferHelper.ReceiveParameters(br);
                    }
                }
                else
                {
                    outParams = _parameterTransferHelper.ReceiveParameters(_binReader);
                }

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
                    }
                    finally
                    {
                        if (null != _binWriter) _binWriter.Dispose();
                        if (null != _binReader) _binReader.Dispose();
                    }
                }
            }
        }

        #endregion
    }
}
