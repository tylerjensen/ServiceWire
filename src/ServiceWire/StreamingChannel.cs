using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Authentication;
using ServiceWire.ZeroKnowledge;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class StreamingChannel : Channel, IDvChannel
    {
        private readonly object _syncRoot = new object();
        protected BinaryReader _binReader;
        protected BinaryWriter _binWriter;
        protected Stream _stream;
        private readonly ParameterTransferHelper _parameterTransferHelper;
        private ServiceSyncInfo _syncInfo;
        private ZkCrypto _zkCrypto;

        // keep cached sync info to avoid redundant wire trips
        private static readonly ConcurrentDictionary<Type, ServiceSyncInfo> SyncInfoCache = new ConcurrentDictionary<Type, ServiceSyncInfo>(); 

        public StreamingChannel()
        {
            if (null == _serializer) _serializer = new DefaultSerializer();
            _parameterTransferHelper = new ParameterTransferHelper(_serializer);
        }

        /// <summary>
        /// Returns true if client is connected to the server.
        /// </summary>
        public virtual bool IsConnected => false;

        /// <summary>
        /// This method asks the server for a list of identifiers paired with method
        /// names and -parameter types. This is used when invoking methods server side.
        /// </summary>
        protected override void SyncInterface(Type serviceType, 
            string username = null, string password = null)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var sw = Stopwatch.StartNew();
                _logger.Debug("Zk authentiation started for: {0}, {1}", username, password);
                //do zk protocol authentication
                var sr = new ZkProtocol();

                // Step 1. Client sends username and ephemeral hash of random number.
                var aRand = sr.CryptRand();
                var aClientEphemeral = sr.GetClientEphemeralA(aRand);
                
                // send username and aClientEphemeral to server
                _binWriter.Write((int)MessageType.ZkInitiate);
                _binWriter.Write(username);
                _logger.Debug("username sent to server: {0}", username);

                _binWriter.Write(aClientEphemeral); //always 32 bytes
                _logger.Debug("ClientEphemeral (A) sent to server: {0}", Convert.ToBase64String(aClientEphemeral));

                // get response from server
                var userFound = _binReader.ReadBoolean();
                if (!userFound)
                {
                    _logger.Debug("User not found. InvalidCredentialException thrown.");
                    throw new InvalidCredentialException("authentication failed");
                }
                var salt = _binReader.ReadBytes(32);
                _logger.Debug("Salt received from server: {0}", Convert.ToBase64String(salt));
                var bServerEphemeral = _binReader.ReadBytes(32);
                _logger.Debug("ServerEphemeral (B) received from server: {0}", Convert.ToBase64String(bServerEphemeral));

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

                _logger.Debug("ClientSessionKey Hash sent to server: {0}", Convert.ToBase64String(clientSessionHash));

                // get response
                var serverVerified = _binReader.ReadBoolean();
                if (!serverVerified)
                {
                    _logger.Debug("Server verification failed. InvalidCredentialException thrown.");
                    throw new InvalidCredentialException("authentication failed");
                }
                var serverSessionHash = _binReader.ReadBytes(32);
                var clientServerSessionHash = sr.ServerCreateSessionHash(aClientEphemeral, 
                    clientSessionHash, clientSessionKey);
                if (!serverSessionHash.IsEqualTo(clientServerSessionHash))
                {
                    _logger.Debug("Server hash mismatch. InvalidCredentialException thrown. Has received: {0}", Convert.ToBase64String(serverSessionHash));
                    throw new InvalidCredentialException("authentication failed");
                }
                _logger.Debug("Server Hash match. Received from server: {0}", Convert.ToBase64String(serverSessionHash));
                _zkCrypto = new ZkCrypto(clientSessionKey, clientScramble);
                _logger.Debug("Zk authentiation completed successfully.");
                sw.Stop();
                _stats.Log("ZkAuthentication", sw.ElapsedMilliseconds);
            }
            
            if (!SyncInfoCache.TryGetValue(serviceType, out _syncInfo))
            {
                //write the message type
                _binWriter.Write((int)MessageType.SyncInterface);
                if (null != _zkCrypto)
                {
                    //sync interface with encryption
                    var assemName = serviceType.ToConfigName();
                    var assemblyNameEncrypted = _zkCrypto.Encrypt(assemName.ConvertToBytes());
                    _binWriter.Write(assemblyNameEncrypted.Length);
                    _binWriter.Write(assemblyNameEncrypted);
                }
                else
                {
                    _binWriter.Write(serviceType.ToConfigName());
                }
                //read sync data
                var len = _binReader.ReadInt32();
                //len is zero when AssemblyQualifiedName not same version or not found
                if (len == 0) throw new TypeAccessException("SyncInterface failed. Type or version of type unknown.");
                var bytes = _binReader.ReadBytes(len);
                if (null != _zkCrypto)
                {
                    _logger.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(bytes));
                    bytes = _zkCrypto.Decrypt(bytes);
                    _logger.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(bytes));
                }
                _syncInfo = _serializer.Deserialize<ServiceSyncInfo>(bytes);
                SyncInfoCache.AddOrUpdate(serviceType, _syncInfo, (t, info) => _syncInfo);
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
                                if (!mdata[i + 1].Equals(si.ParameterTypes[i]))
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
                    _logger.Debug("Unencrypted data sent to server: {0}", Convert.ToBase64String(callData));
                    var encData = _zkCrypto.Encrypt(callData);
                    _binWriter.Write(encData.Length);
                    _binWriter.Write(encData);
                    _logger.Debug("Encrypted data sent to server: {0}", Convert.ToBase64String(encData));
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

                    _logger.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(encData));
                    var data = _zkCrypto.Decrypt(encData);
                    _logger.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(data));

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

                MethodSyncInfo methodSyncInfo = _syncInfo.MethodInfos[ident];
                var returnType = methodSyncInfo.MethodReturnType.ToType();
                if (IsTaskType(returnType) && outParams.Length > 0)
                {
	                if (returnType.IsGenericType)
	                {
		                MethodInfo methodInfo = typeof(Task).GetMethod(nameof(Task.FromResult))
			                .MakeGenericMethod(new[] { returnType.GenericTypeArguments[0] });
		                outParams[0] = methodInfo.Invoke(null, new[] { outParams[0] });
					}
	                else
	                {
		                outParams[0] = Task.CompletedTask;
	                }
				}
				
				if (messageType == MessageType.ThrowException)
                    throw (Exception)outParams[0];

                return outParams;
            }
        }

        private static bool IsTaskType(Type type)
		{
			if (type == typeof(Task))
				return true;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
				return true;

			return false;
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
