using ServiceWire.ZeroKnowledge;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
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
        private bool _useWireV2;
        private bool _syncInfoFromCache;
        private volatile bool _v2Confirmed;
        private int _nextCorrelationId;
        private ServiceSyncInfoCacheKey _syncInfoCacheKey;
        private readonly Dictionary<string, MethodSyncInfo> _methodCache = new Dictionary<string, MethodSyncInfo>(StringComparer.Ordinal);
        private readonly Dictionary<int, Type> _returnTypeCache = new Dictionary<int, Type>();
        private readonly Dictionary<int, Func<object, object>> _taskFromResultCache = new Dictionary<int, Func<object, object>>();

        // keep cached sync info to avoid redundant wire trips
        private static readonly ConcurrentDictionary<ServiceSyncInfoCacheKey, ServiceSyncInfo> SyncInfoCache = new ConcurrentDictionary<ServiceSyncInfoCacheKey, ServiceSyncInfo>();

        public StreamingChannel(ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _parameterTransferHelper = new ParameterTransferHelper(_serializer, _compressor);
        }

        public static void ClearCachedSyncInfo()
        {
            SyncInfoCache.Clear();
        }

        protected virtual IChannelIdentifier ChannelIdentifier { get; }

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
            var debugEnabled = _logger.IsDebugEnabled();
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var sw = _stats.IsEnabled() ? Stopwatch.StartNew() : null;
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
                if (debugEnabled) _logger.Debug("ClientEphemeral (A) sent to server: {0}", Convert.ToBase64String(aClientEphemeral));

                // get response from server
                var userFound = _binReader.ReadBoolean();
                if (!userFound)
                {
                    _logger.Debug("User not found. InvalidCredentialException thrown.");
                    throw new InvalidCredentialException("authentication failed");
                }
                var salt = _binReader.ReadBytes(32);
                if (debugEnabled) _logger.Debug("Salt received from server: {0}", Convert.ToBase64String(salt));
                var bServerEphemeral = _binReader.ReadBytes(32);
                if (debugEnabled) _logger.Debug("ServerEphemeral (B) received from server: {0}", Convert.ToBase64String(bServerEphemeral));

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

                if (debugEnabled) _logger.Debug("ClientSessionKey Hash sent to server: {0}", Convert.ToBase64String(clientSessionHash));

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
                    if (debugEnabled) _logger.Debug("Server hash mismatch. InvalidCredentialException thrown. Has received: {0}", Convert.ToBase64String(serverSessionHash));
                    throw new InvalidCredentialException("authentication failed");
                }
                if (debugEnabled) _logger.Debug("Server Hash match. Received from server: {0}", Convert.ToBase64String(serverSessionHash));
                _zkCrypto = new ZkCrypto(clientSessionKey, clientScramble);
                _logger.Debug("Zk authentiation completed successfully.");
                if (sw != null)
                {
                    sw.Stop();
                    _stats.Log("ZkAuthentication", sw.ElapsedMilliseconds);
                }
            }

            var serviceSyncInfoCacheKey = new ServiceSyncInfoCacheKey(serviceType, ChannelIdentifier);
            _syncInfoCacheKey = serviceSyncInfoCacheKey;

            _syncInfoFromCache = SyncInfoCache.TryGetValue(serviceSyncInfoCacheKey, out _syncInfo);
            if (!_syncInfoFromCache)
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
                } else
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
                    if (debugEnabled) _logger.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(bytes));
                    bytes = _zkCrypto.Decrypt(bytes);
                    if (debugEnabled) _logger.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(bytes));
                }
                _syncInfo = _serializer.Deserialize<ServiceSyncInfo>(bytes);
                SyncInfoCache.AddOrUpdate(serviceSyncInfoCacheKey, _syncInfo, (t, info) => _syncInfo);
            }

            //a pre-7.0 server leaves CapabilityFlags at 0 and the channel stays on the v1 wire
            _useWireV2 = 0 != (_syncInfo.CapabilityFlags & (int)ProtocolCapabilities.WireV2);
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
                var debugEnabled = _logger.IsDebugEnabled();
                var methodSyncInfo = ResolveMethod(metaData);
                if (_useWireV2)
                    return InvokeMethodV2(methodSyncInfo, useCrypto, debugEnabled, parameters);
                var ident = methodSyncInfo.MethodIdent;

                //write the message type
                _binWriter.Write((int)MessageType.MethodInvocation);

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
                    if (debugEnabled) _logger.Debug("Unencrypted data sent to server: {0}", Convert.ToBase64String(callData));
                    var encData = _zkCrypto.Encrypt(callData);
                    _binWriter.Write(encData.Length);
                    _binWriter.Write(encData);
                    if (debugEnabled) _logger.Debug("Encrypted data sent to server: {0}", Convert.ToBase64String(encData));
                } else
                {
                    //send the parameters
                    _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                        _syncInfo.CompressionThreshold,
                        _binWriter,
                        parameters);
                }

                _binWriter.Flush();

                // Read the result of the invocation.
                MessageType messageType = (MessageType)_binReader.ReadInt32();
                if (messageType == MessageType.UnknownMethod)
                    throw new Exception("Unknown method.");

                object[] outParams;
                if (useCrypto)
                {
                    var len = _binReader.ReadInt32();
                    var encData = _binReader.ReadBytes(len);

                    if (debugEnabled) _logger.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(encData));
                    var data = _zkCrypto.Decrypt(encData);
                    if (debugEnabled) _logger.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(data));

                    using (var ms = new MemoryStream(data))
                    using (var br = new BinaryReader(ms))
                    {
                        outParams = _parameterTransferHelper.ReceiveParameters(br);
                    }
                } else
                {
                    outParams = _parameterTransferHelper.ReceiveParameters(_binReader);
                }

                if (messageType == MessageType.ThrowException)
                    throw (Exception)outParams[0];

                return ApplyTaskConversion(methodSyncInfo, outParams);
            }
        }

        /// <summary>
        /// Sends a framed MethodInvocation2 request and reads its Response2 frame.
        /// Only called after the server advertised ProtocolCapabilities.WireV2.
        /// </summary>
        private object[] InvokeMethodV2(MethodSyncInfo methodSyncInfo, bool useCrypto, bool debugEnabled, object[] parameters)
        {
            //build the payload first so the frame length is known
            byte[] payload;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                if (useCrypto)
                {
                    byte[] callData;
                    using (var innerMs = new MemoryStream())
                    using (var innerBw = new BinaryWriter(innerMs))
                    {
                        _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                            _syncInfo.CompressionThreshold, innerBw, WireVersion.V2, parameters);
                        callData = innerMs.ToArray();
                    }
                    if (debugEnabled) _logger.Debug("Unencrypted data sent to server: {0}", Convert.ToBase64String(callData));
                    var encData = _zkCrypto.Encrypt(callData);
                    bw.Write(encData.Length);
                    bw.Write(encData);
                    if (debugEnabled) _logger.Debug("Encrypted data sent to server: {0}", Convert.ToBase64String(encData));
                } else
                {
                    _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                        _syncInfo.CompressionThreshold, bw, WireVersion.V2, parameters);
                }
                bw.Flush();
                payload = ms.ToArray();
            }

            const int requestHeaderLength = 13; //correlationId + serviceKey + methodIdent + flags
            var correlationId = System.Threading.Interlocked.Increment(ref _nextCorrelationId);
            try
            {
                _binWriter.Write((int)MessageType.MethodInvocation2);
                _binWriter.Write(requestHeaderLength + payload.Length);
                _binWriter.Write(correlationId);
                _binWriter.Write(_syncInfo.ServiceKeyIndex);
                _binWriter.Write(methodSyncInfo.MethodIdent);
                _binWriter.Write((byte)(useCrypto ? 1 : 0));
                _binWriter.Write(payload);
                _binWriter.Flush();

                var messageType = (MessageType)_binReader.ReadInt32();
                if (messageType != MessageType.Response2)
                    throw new IOException(string.Format("Expected Response2 but received message type {0}; stream is desynchronized.", messageType));
                var frameLength = _binReader.ReadInt32();
                var responseCorrelationId = _binReader.ReadInt32();
                var status = _binReader.ReadByte();
                var frameFlags = _binReader.ReadByte();
                const int responseHeaderLength = 6; //correlationId + status + flags
                var responsePayloadLength = frameLength - responseHeaderLength;
                var responsePayload = responsePayloadLength > 0 ? _binReader.ReadBytes(responsePayloadLength) : new byte[0];
                if (responseCorrelationId != correlationId)
                    throw new IOException("Response correlation id does not match the request; stream is desynchronized.");
                _v2Confirmed = true;

                if (status == 2) throw new Exception("Unknown method.");

                object[] outParams;
                using (var ms = new MemoryStream(responsePayload))
                using (var br = new BinaryReader(ms))
                {
                    if (0 != (frameFlags & 1))
                    {
                        var len = br.ReadInt32();
                        var encData = br.ReadBytes(len);
                        if (debugEnabled) _logger.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(encData));
                        var data = _zkCrypto.Decrypt(encData);
                        if (debugEnabled) _logger.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(data));
                        using (var decMs = new MemoryStream(data))
                        using (var decBr = new BinaryReader(decMs))
                        {
                            outParams = _parameterTransferHelper.ReceiveParameters(decBr, WireVersion.V2);
                        }
                    } else
                    {
                        outParams = _parameterTransferHelper.ReceiveParameters(br, WireVersion.V2);
                    }
                }

                if (status == 1) throw (Exception)outParams[0];

                return ApplyTaskConversion(methodSyncInfo, outParams);
            }
            catch (Exception e) when (!_v2Confirmed && _syncInfoFromCache && (e is EndOfStreamException || e is IOException))
            {
                //the WireV2 capability came from the process-wide cache, and the very first
                //v2 exchange died before any response byte: the server was likely replaced
                //by one that does not understand v2 (it drops the connection after reading
                //only the message type, before executing anything). Evict the stale entry
                //so the next channel renegotiates, and surface a descriptive error.
                SyncInfoCache.TryRemove(_syncInfoCacheKey, out _);
                throw new InvalidOperationException(
                    "The server rejected a v2 framed request that was sent based on cached capability data. " +
                    "The stale cache entry has been evicted; create a new channel to renegotiate.", e);
            }
        }

        private object[] ApplyTaskConversion(MethodSyncInfo methodSyncInfo, object[] outParams)
        {
            var returnType = GetReturnType(methodSyncInfo);
            if (IsTaskType(returnType) && outParams.Length > 0)
            {
                if (returnType.IsGenericType)
                {
                    var taskFromResult = GetTaskFromResultConverter(methodSyncInfo.MethodIdent, returnType);
                    outParams[0] = taskFromResult(outParams[0]);
                } else
                {
                    outParams[0] = Task.CompletedTask;
                }
            }
            return outParams;
        }

        private MethodSyncInfo ResolveMethod(string metaData)
        {
            MethodSyncInfo cachedMethod;
            if (_methodCache.TryGetValue(metaData, out cachedMethod))
                return cachedMethod;

            var mdata = metaData.Split('|');
            for (int index = 0; index < _syncInfo.MethodInfos.Length; index++)
            {
                var method = _syncInfo.MethodInfos[index];
                if (method.MethodName != mdata[0] || mdata.Length - 1 != method.ParameterTypes.Length)
                    continue;

                var matchingParameterTypes = true;
                for (int i = 0; i < method.ParameterTypes.Length; i++)
                {
                    if (!mdata[i + 1].Equals(method.ParameterTypes[i]))
                    {
                        matchingParameterTypes = false;
                        break;
                    }
                }

                if (matchingParameterTypes)
                {
                    _methodCache.Add(metaData, method);
                    return method;
                }
            }

            throw new Exception(string.Format("Cannot match method '{0}' to its server side equivalent", mdata[0]));
        }

        private Type GetReturnType(MethodSyncInfo methodSyncInfo)
        {
            Type returnType;
            if (_returnTypeCache.TryGetValue(methodSyncInfo.MethodIdent, out returnType))
                return returnType;

            returnType = methodSyncInfo.MethodReturnType.ToType();
            if (returnType != null)
                _returnTypeCache.Add(methodSyncInfo.MethodIdent, returnType);
            return returnType;
        }

        private Func<object, object> GetTaskFromResultConverter(int methodIdent, Type returnType)
        {
            Func<object, object> converter;
            if (_taskFromResultCache.TryGetValue(methodIdent, out converter))
                return converter;

            converter = MethodInvokerCompiler.CompileTaskFromResult(returnType);
            _taskFromResultCache.Add(methodIdent, converter);
            return converter;
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
