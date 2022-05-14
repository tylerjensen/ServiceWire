using NetMQ;
using NetMQ.Sockets;
using ServiceWire.Messaging;
using ServiceWire.ZeroKnowledge;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class Host : IDisposable
    {
        protected volatile bool _isOpen;
        protected volatile bool _continueListening = true;
        protected bool _useCompression = false; //default is false
        protected int _compressionThreshold = 131072; //128KB
        protected readonly ILog _log;
        protected readonly IStats _stats;
        protected readonly ISerializer _serializer;
        protected readonly ICompressor _compressor;
        protected readonly IZkRepository _zkRepository;
        private volatile bool _requireZk = false;

        protected ConcurrentDictionary<string, int> _serviceKeys = new ConcurrentDictionary<string, int>();
        protected ConcurrentDictionary<int, ServiceInstance> _services = new ConcurrentDictionary<int, ServiceInstance>();
        protected readonly ParameterTransferHelper _parameterTransferHelper;

        protected readonly IMsgHost _host;
        protected readonly string _connectionString;

        public Host(string connectionString, int sessionTimeoutMins, ISerializer serializer, ICompressor compressor, ILog log, IStats stats, IZkRepository zkRepository)
        {
            _connectionString = connectionString ?? "inproc://servicewire";
            _serializer = serializer ?? new DefaultSerializer();
            _compressor = compressor ?? new DefaultCompressor();
            _log = log ?? new NullLogger();
            _stats = stats ?? new NullStats();
            _parameterTransferHelper = new ParameterTransferHelper(_serializer, _compressor);

            _zkRepository = zkRepository ?? new ZkNullRepository();

            _host = new MsgHost(_connectionString, _zkRepository, _log, _stats, sessionTimeoutMins);
            _host.ZkClientSessionEstablishedEvent += ZkClientSessionEstablishedEvent;
            _host.ReceivedHeartbeatEvent += ReceivedHeartbeatEvent;
            _host.MessageSentFailure += MessageSentFailure;
            _host.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            ProcessRequest(e.Message);
        }

        private void MessageSentFailure(object sender, MessageEventFailureArgs e)
        {
            //TODO - decide how to surface or log this
        }

        private void ReceivedHeartbeatEvent(object sender, MessageEventArgs e)
        {
            //TODO - decide how to surface or log this
        }

        private void ZkClientSessionEstablishedEvent(object sender, MessageEventArgs e)
        {
            _requireZk = true;
            //TODO - decide how to surface or log this
        }


        public IZkRepository ZkRepository
        {
            get
            {
                return _zkRepository;
            }
        }

        public IStats Stats
        {
            get
            {
                return _stats;
            }
        }

        public ILog Log
        {
            get
            {
                return _log;
            }
        }

        protected bool Continue
        {
            get
            {
                return _continueListening;
            }
            set
            {
                _continueListening = value;
            }
        }

        /// <summary>
        /// Enable parameter compression. Default is false. There is a performance penalty
        /// when using compression that should be weighed against network transmission
        /// costs of large data parameters being serialized across the wire.
        /// </summary>
        public bool UseCompression
        {
            get
            {
                return _useCompression;
            }
            set
            {
                _useCompression = value;
            }
        }

        /// <summary>
        /// Compression, if enabled, occurs once a parameter exceeds this value
        /// in the number of bytes. Strings, byte and char arrays, and complex serialized types.
        /// The minimum is 1024 bytes. The default is 128KB.
        /// </summary>
        public int CompressionThreshold
        {
            get
            {
                return _compressionThreshold;
            }
            set
            {
                _compressionThreshold = value;
                if (_compressionThreshold < 1024) _compressionThreshold = 1024;
            }
        }

        /// <summary>
        /// Add this service implementation to the host.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="service">The singleton implementation.</param>
        public void AddService<TService>(TService service) where TService : class
        {
            try
            {
                if (_isOpen) throw new Exception("Service cannot be added after the host is opened.");
                var serviceType = typeof(TService);
                if (!serviceType.IsInterface) throw new ArgumentException("TService must be an interface.", "TService");
                //serviceType.ValidateServiceInterface(); //throws if one class in the interface or its members is not serializable
                var serviceKey = serviceType.ToConfigName(); // serviceType.AssemblyQualifiedName ?? serviceType.FullName;
                if (_serviceKeys.ContainsKey(serviceKey)) throw new Exception("Service already added. Only one instance allowed.");
                int keyIndex = _serviceKeys.Count;
                _serviceKeys.TryAdd(serviceKey, keyIndex);
                var instance = CreateMethodMap(keyIndex, serviceType, service);
                _services.TryAdd(keyIndex, instance);
            }
            catch (Exception e)
            {
                _log.Fatal("AddServive exception on {0}. Error: {1}", service.GetType(), e.ToString().Flatten());
                throw;
            }
        }

        /// <summary>
        /// Loads all methods from interfaces and assigns an identifier
        /// to each. These are later synchronized with the client.
        /// </summary>
        private ServiceInstance CreateMethodMap(int keyIndex, Type serviceType, object service)
        {
            var instance = new ServiceInstance()
            {
                KeyIndex = keyIndex,
                InterfaceType = serviceType,
                InterfaceMethods = new ConcurrentDictionary<int, MethodInfo>(),
                MethodParametersByRef = new ConcurrentDictionary<int, bool[]>(),
                SingletonInstance = service
            };

            var currentMethodIdent = 0;
            if (serviceType.IsInterface)
            {
                var methodInfos = serviceType.GetMethods();
                foreach (var mi in methodInfos)
                {
                    instance.InterfaceMethods.TryAdd(currentMethodIdent, mi);
                    var parameterInfos = mi.GetParameters();
                    var isByRef = new bool[parameterInfos.Length];
                    for (int i = 0; i < isByRef.Length; i++)
                        isByRef[i] = parameterInfos[i].ParameterType.IsByRef;
                    instance.MethodParametersByRef.TryAdd(currentMethodIdent, isByRef);
                    currentMethodIdent++;
                }
            }

            var interfaces = serviceType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                var methodInfos = interfaceType.GetMethods();
                foreach (var mi in methodInfos)
                {
                    instance.InterfaceMethods.TryAdd(currentMethodIdent, mi);
                    var parameterInfos = mi.GetParameters();
                    var isByRef = new bool[parameterInfos.Length];
                    for (int i = 0; i < isByRef.Length; i++)
                        isByRef[i] = parameterInfos[i].ParameterType.IsByRef;
                    instance.MethodParametersByRef.TryAdd(currentMethodIdent, isByRef);
                    currentMethodIdent++;
                }
            }

            //Create a list of sync infos from the dictionary
            var syncSyncInfos = new List<MethodSyncInfo>();
            foreach (var kvp in instance.InterfaceMethods)
            {
                var parameters = kvp.Value.GetParameters();
                var parameterTypes = new string[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    parameterTypes[i] = parameters[i].ParameterType.ToConfigName();
                syncSyncInfos.Add(new MethodSyncInfo
                {
                    MethodIdent = kvp.Key,
                    MethodName = kvp.Value.Name,
                    MethodReturnType = kvp.Value.ReturnType.ToConfigName(),
                    ParameterTypes = parameterTypes
                });
            }

            var serviceSyncInfo = new ServiceSyncInfo
            {
                ServiceKeyIndex = keyIndex,
                CompressionThreshold = _compressionThreshold,
                UseCompression = _useCompression,
                MethodInfos = syncSyncInfos.ToArray()
            };
            instance.ServiceSyncInfo = serviceSyncInfo;
            return instance;
        }


        /// <summary>
        /// This method handles all requests.
        /// </summary>
        /// <param name="message">The request message.</param>
        private void ProcessRequest(Message message)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                //read message type
                var messageType = (MessageType)BitConverter.ToInt32(message.Frames[0], 0);
                switch (messageType)
                {
                    case MessageType.SyncInterface:
                        ProcessSync(message, sw);
                        break;
                    case MessageType.MethodInvocation:
                        ProcessInvocation(message, sw);
                        break;
                    case MessageType.TerminateConnection:
                        _host.RemoveSession(message.ClientId);
                        break;
                    default:
                        //TODO - log unknown messageType?
                        break;
                }
            }
            catch (Exception e) //do not resume operation on this thread if any errors are unhandled.
            {
                _log.Error("Error in ProcessRequest: {0}", e.ToString().Flatten());
            }
            sw.Stop();
        }

        private void ProcessSync(Message message, Stopwatch sw)
        {
            var msgSession = _host.GetSession(message.ClientId);
            var session = msgSession?.Session;
            var retMsg = new Message { ClientId = message.ClientId, Frames = new List<byte[]>() };
            retMsg.Frames.Add(BitConverter.GetBytes((int)MessageType.SyncInterface));

            var syncCat = "Sync";

            string serviceTypeName;
            if (_requireZk)
            {
                //use session and encryption - if throws should not have gotten this far
                var bytes = message.Frames[1];
                var data = session.Crypto.Decrypt(bytes);
                serviceTypeName = data.ConvertToString();
            } else
            {
                serviceTypeName = message.Frames[1].ConvertToString();
            }

            if (_serviceKeys.TryGetValue(serviceTypeName, out var serviceKey))
            {
                if (_services.TryGetValue(serviceKey, out var instance))
                {
                    syncCat = instance.InterfaceType.Name;
                    //Create a list of sync infos from the dictionary
                    byte[] syncBytes = null;
                    try
                    {
                        //if the serializer fails, we need to send 0 to client to indicate sync error
                        syncBytes = _serializer.Serialize(instance.ServiceSyncInfo);
                    }
                    catch (Exception e)
                    {
                        //return zero to indicate failure to client to avoid EOS error on client
                        retMsg.Frames.Add(BitConverter.GetBytes(0));
                        _log.Debug("SyncInterface error {0}.", e);
                    }
                    if (null != syncBytes)
                    {
                        retMsg.Frames.Add(BitConverter.GetBytes(1)); //indicate success
                        if (_requireZk)
                        {
                            _log.Debug("Unencrypted data sent to server: {0}", Convert.ToBase64String(syncBytes));
                            var encData = session.Crypto.Encrypt(syncBytes);
                            retMsg.Frames.Add(encData);
                            _log.Debug("Encrypted data sent server: {0}", Convert.ToBase64String(encData));
                        }
                        else
                        {
                            retMsg.Frames.Add(syncBytes);
                        }
                    }
                }
            } else
            {
                //return zero to indicate type or version of type not found
                retMsg.Frames.Add(BitConverter.GetBytes(0));
            }
            _host.Send(retMsg);
            _log.Debug("SyncInterface for {0} in {1}ms.", syncCat, sw.ElapsedMilliseconds);
        }

        private void ProcessInvocation(Message message, Stopwatch sw)
        {
            var msgSession = _host.GetSession(message.ClientId);
            var session = msgSession?.Session;
            var retMsg = new Message { ClientId = message.ClientId, Frames = new List<byte[]>() };

            //read service instance key
            var cat = "unknown";
            var stat = "MethodInvocation";
            //start with 2 because 1 is the invokeId
            int invokedServiceKey = BitConverter.ToInt32(message.Frames[2], 0);
            if (_services.TryGetValue(invokedServiceKey, out var invokedInstance))
            {
                cat = invokedInstance.InterfaceType.Name;
                //read the method identifier
                int methodHashCode = BitConverter.ToInt32(message.Frames[3], 0);
                if (invokedInstance.InterfaceMethods.ContainsKey(methodHashCode))
                {
                    invokedInstance.InterfaceMethods.TryGetValue(methodHashCode, out var method);
                    stat = method.Name;

                    invokedInstance.MethodParametersByRef.TryGetValue(methodHashCode, out var isByRef);

                    //read parameter data
                    object[] parameters;
                    byte[] data;
                    if (_requireZk)
                    {
                        var encData = message.Frames[4];
                        _log.Debug("Encrypted data received from server: {0}", Convert.ToBase64String(encData));
                        data = session.Crypto.Decrypt(encData);
                        _log.Debug("Decrypted data received from server: {0}", Convert.ToBase64String(data));
                    } else
                    {
                        // TODO - modify ReceiveParameters to read from frames rather than stream of bytes maybe
                        data = message.Frames[4];
                    }
                    using (var ms = new MemoryStream(data))
                    using (var br = new BinaryReader(ms))
                    {
                        parameters = _parameterTransferHelper.ReceiveParameters(br);
                    }

                    //invoke the method
                    object[] returnParameters;
                    var returnMessageType = MessageType.ReturnValues;
                    try
                    {
                        object returnValue = method.Invoke(invokedInstance.SingletonInstance, parameters);
                        if (returnValue is Task task)
                        {
                            task.GetAwaiter().GetResult();
                            var prop = task.GetType().GetProperty("Result");
                            returnValue = prop?.GetValue(task);
                        }
                        //the result to the client is the return value (null if void) and the input parameters
                        returnParameters = new object[1 + parameters.Length];
                        returnParameters[0] = returnValue;
                        for (int i = 0; i < parameters.Length; i++)
                            returnParameters[i + 1] = isByRef[i] ? parameters[i] : null;
                    }
                    catch (Exception ex)
                    {
                        //an exception was caught. Rethrow it client side
                        returnParameters = new object[] { (ex is TargetInvocationException && ex.InnerException != null) ? ex.InnerException : ex };
                        returnMessageType = MessageType.ThrowException;
                    }

                    //send the result back to the client
                    // (1) write the message type
                    retMsg.Frames.Add(BitConverter.GetBytes((int)returnMessageType));
                    retMsg.Frames.Add(message.Frames[1]); //invokeId

                    // (2) write the return parameters
                    byte[] rawResponseData;
                    byte[] responseData;
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        _parameterTransferHelper.SendParameters(
                            invokedInstance.ServiceSyncInfo.UseCompression,
                            invokedInstance.ServiceSyncInfo.CompressionThreshold,
                            bw,
                            returnParameters);
                        rawResponseData = ms.ToArray();
                    }
                    if (_requireZk)
                    {
                        _log.Debug("Unencrypted data sent server: {0}", Convert.ToBase64String(rawResponseData));
                        responseData = session.Crypto.Encrypt(rawResponseData);
                        _log.Debug("Encrypted data sent server: {0}", Convert.ToBase64String(responseData));
                    } 
                    else
                    {
                        responseData = rawResponseData;
                    }
                    retMsg.Frames.Add(responseData);
                }
                else
                {
                    retMsg.Frames.Add(BitConverter.GetBytes((int)MessageType.UnknownMethod));
                    retMsg.Frames.Add(message.Frames[1]); //invokeId
                }
            } else
            {
                retMsg.Frames.Add(BitConverter.GetBytes((int)MessageType.UnknownMethod));
                retMsg.Frames.Add(message.Frames[1]); //invokeId
            }

            //flush
            _host.Send(retMsg);
            _stats.Log(cat, stat, sw.ElapsedMilliseconds);
        }

        #region IDisposable Members

        private bool _disposed = false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true; //prevent second call to Dispose
            if (disposing)
            {
                if (_log is Logger log) log.FlushLog();
                if (_stats is Stats stat) stat.FlushLog();
                _isOpen = false;
                Continue = false;
                foreach (var instance in _services)
                {
                    if (instance.Value.SingletonInstance is IDisposable disposable) disposable.Dispose();
                }
                if (null != _host) _host.Dispose();
            }
        }

        #endregion
    }
}
