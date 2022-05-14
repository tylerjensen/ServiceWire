using NetMQ;
using NetMQ.Sockets;
using ServiceWire.Messaging;
using ServiceWire.ZeroKnowledge;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class StreamingChannel : Channel, IDvChannel
    {
        private readonly object _syncRoot = new object();
        private readonly ParameterTransferHelper _parameterTransferHelper;
        private ServiceSyncInfo _syncInfo;
        private bool _connectionSecured = false;
        private bool _shouldSecureConnection = false;
        private bool _syncInfoCompleted = false;

        // keep cached sync info to avoid redundant wire trips
        private static readonly ConcurrentDictionary<Type, ServiceSyncInfo> SyncInfoCache = new ConcurrentDictionary<Type, ServiceSyncInfo>();

        // track invocations to match up response messages
        private readonly ConcurrentDictionary<Guid, Message> _invokeResponses;

        private readonly string _identity;
        private readonly string _identityKey;
        private readonly string _connectionString;
        private readonly IMsgClient _client;

        public StreamingChannel(ISerializer serializer, ICompressor compressor, string connectionString, 
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000)
            : base(serializer, compressor, log, stats, invokeTimeoutMs)
        {
            _parameterTransferHelper = new ParameterTransferHelper(_serializer, _compressor);
            _identity = identity;
            _identityKey = identityKey;
            _connectionString = connectionString;
            _shouldSecureConnection = (null != _identity && null != _identityKey);
            _invokeResponses = new ConcurrentDictionary<Guid, Message>();

            _client = new MsgClient(_connectionString, _identity, _identityKey, _log, _stats);
            _client.EcryptionProtocolEstablished += EcryptionProtocolEstablished;
            _client.EcryptionProtocolFailed += EcryptionProtocolFailed;
            _client.HeartbeatReceived += HeartbeatReceived;
            _client.InvalidMessageReceived += InvalidMessageReceived;
            _client.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                var messageType = (MessageType)BitConverter.ToInt32(e.Message.Frames[0], 0);
                switch (messageType)
                {
                    case MessageType.SyncInterface:
                        CompleteSyncInterface(e.Message);
                        break;
                    case MessageType.ReturnValues:
                    case MessageType.UnknownMethod:
                    case MessageType.ThrowException:
                        var invokeId = new Guid(e.Message.Frames[1]);
                        _invokeResponses.TryAdd(invokeId, e.Message);
                        break;
                    case MessageType.TerminateConnection:
                        _log.Info("Client terminated by server.");
                        _client.Dispose();
                        break;
                    default:
                        //TODO - log unknown messageType?
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error in handling MessageReceived: {0}", e.ToString().Flatten());
            }
        }

        private void InvalidMessageReceived(object sender, MessageEventArgs e)
        {
            //TODO decide what to do with this
            _log.Error("Invalid Message Received with {0}.", e.Message);
        }

        private void HeartbeatReceived(object sender, MessageEventArgs e)
        {
            //TODO decide what to do with this
            IsConnected = true;
            _log.Info("Heartbeat Received with {0}.", e.Message);
        }

        private void EcryptionProtocolFailed(object sender, ProtocolFailureEventArgs e)
        {
            //TODO decide what to do with this
            _log.Error("Encryption Protocol failed with {0}.", e.Message);
        }

        private void EcryptionProtocolEstablished(object sender, EventArgs e)
        {
            //TODO decide what to do with this
            IsConnected = true;
            _log.Info("Ecryption Protocol Established {0}.", _client.ClientId);
        }

        /// <summary>
        /// Returns true if client is connected to the server.
        /// </summary>
        public bool IsConnected { get; set; }


        /// <summary>
        /// This method asks the server for a list of identifiers paired with method
        /// names and -parameter types. This is used when invoking methods server side.
        /// </summary>
        protected override void SyncInterface()
        {
            if (_shouldSecureConnection && !_connectionSecured)
            {
                _client.SecureConnection(true);
                _connectionSecured = true;
            }

            if (!SyncInfoCache.TryGetValue(_serviceType, out _syncInfo))
            {
                //write the message type
                var frames = new List<byte[]>();
                
                frames.Add(BitConverter.GetBytes((int)MessageType.SyncInterface));
                if (_connectionSecured)
                {
                    //sync interface with encryption
                    var assemName = _serviceType.ToConfigName();
                    var assemblyNameEncrypted = _client.Session.Crypto.Encrypt(assemName.ConvertToBytes());
                    frames.Add(assemblyNameEncrypted);
                } else
                {
                    frames.Add(_serviceType.ToConfigName().ConvertToBytes());
                }
                _client.Send(frames);
            }
        }

        protected void CompleteSyncInterface(Message message)
        {
            //read sync data
            var success = BitConverter.ToInt32(message.Frames[1], 0) == 1;
            //len is zero when AssemblyQualifiedName not same version or not found
            //TODO - explore whether this should just log or raise rather than throw.
            if (!success) throw new TypeAccessException("SyncInterface failed. Type or version of type unknown.");
            var rawBytes = message.Frames[2];
            byte[] data;
            if (_connectionSecured)
            {
                _log.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(rawBytes));
                data = _client.Session.Crypto.Decrypt(rawBytes);
                _log.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(data));
            }
            else
            {
                data = rawBytes;
            }
            _syncInfo = _serializer.Deserialize<ServiceSyncInfo>(data);
            SyncInfoCache.AddOrUpdate(_serviceType, _syncInfo, (t, info) => _syncInfo);
            _syncInfoCompleted = true;
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
            if (!_syncInfoCompleted) SpinWait.SpinUntil(() => _syncInfoCompleted);

            //prevent call to invoke method on more than one thread at a time
            var mdata = metaData.Split('|');

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

            var frames = new List<byte[]>();
            //write the message type
            frames.Add(BitConverter.GetBytes((int)MessageType.MethodInvocation));

            //write the unique invocation id and store it for retrieval 
            var invokeId = Guid.NewGuid();
            frames.Add(invokeId.ToByteArray());

            //write service key index
            frames.Add(BitConverter.GetBytes(_syncInfo.ServiceKeyIndex));

            //write the method ident to the server
            frames.Add(BitConverter.GetBytes(ident));

            //send the parameters
            byte[] data;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                //send the parameters
                _parameterTransferHelper.SendParameters(_syncInfo.UseCompression,
                    _syncInfo.CompressionThreshold,
                    bw,
                    parameters);
                data = ms.ToArray();
            }
            if (_connectionSecured)
            {
                _log.Debug("Unencrypted data sent to server: {0}", Convert.ToBase64String(data));
                var encData = _client.Session.Crypto.Encrypt(data);
                frames.Add(encData);
                _log.Debug("Encrypted data sent to server: {0}", Convert.ToBase64String(encData));
            }
            else
            {
                frames.Add(data);
            }
            _log.Debug("InvokeMethod called for {0} with invoke id:{1}", mdata[0], invokeId);
            _client.Send(frames);

            //spin wait for response
            var spinWait = new SpinWait();
            var sw = Stopwatch.StartNew();
            Message response = null;
            while (sw.ElapsedMilliseconds < _invokeTimeoutMs)
            {
                spinWait.SpinOnce();
                if (_invokeResponses.TryRemove(invokeId, out response))
                {
                    sw.Stop();
                    break;
                }
            }
            if (null == response) throw new TimeoutException("InvokeMethod timeout on " + mdata[0] + " for invoke id:" + invokeId.ToString());

            //start with frame 2 (0 was msg type, 1 was invokeId)
            MessageType messageType = (MessageType)BitConverter.ToInt32(response.Frames[0], 0);
            if (messageType == MessageType.UnknownMethod)
                throw new Exception("Unknown method.");

            object[] outParams;
            var rawRespData = response.Frames[2];
            byte[] respData;
            if (_connectionSecured)
            {
                _log.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(rawRespData));
                respData = _client.Session.Crypto.Decrypt(rawRespData);
                _log.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(respData));
            }
            else
            {
                respData = rawRespData;
            }
            using (var ms = new MemoryStream(respData))
            using (var br = new BinaryReader(ms))
            {
                outParams = _parameterTransferHelper.ReceiveParameters(br);
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
                } else
                {
                    outParams[0] = Task.CompletedTask;
                }
            }

            if (messageType == MessageType.ThrowException)
                throw (Exception)outParams[0];

            return outParams;
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
                    IsConnected = false;
                    if (null != _client) _client.Dispose();
                }
            }
        }

        #endregion
    }
}
