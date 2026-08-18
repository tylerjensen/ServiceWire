using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceWire.Aspects
{
    public class InterceptChannel : Channel
    {
        private InterceptPoint _interceptPoint;
        private ServiceInstance _serviceInstance;
        private readonly ConcurrentDictionary<string, MethodSyncInfo> _methodCache = new ConcurrentDictionary<string, MethodSyncInfo>(StringComparer.Ordinal);

        public InterceptPoint InterceptPoint { get { return _interceptPoint; } }

        public InterceptChannel(Type interceptedType, InterceptPoint interceptPoint, ISerializer serializer, ICompressor compressor,
            ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _serviceType = interceptedType;
            _interceptPoint = interceptPoint;
            CreateMethodMap();
        }

        protected override void SyncInterface(Type interceptedType,
            string username = null, string password = null)
        {
            //do nothing in this channel
        }

        /// <summary>
        /// Loads all methods from interfaces and assigns an identifier
        /// to each. These are later synchronized with the client.
        /// </summary>
        private void CreateMethodMap()
        {
            _serviceInstance = new ServiceInstance()
            {
                KeyIndex = 0, //only one per intercepted interface
                InterfaceType = _serviceType,
                InterfaceMethods = new ConcurrentDictionary<int, MethodInfo>(),
                MethodParametersByRef = new ConcurrentDictionary<int, bool[]>(),
                CompiledMethods = new ConcurrentDictionary<int, Func<object, object[], object>>(),
                SingletonInstance = _interceptPoint.Target
            };

            var currentMethodIdent = 0;
            if (_serviceType.IsInterface)
            {
                var methodInfos = _serviceType.GetMethods();
                foreach (var mi in methodInfos)
                {
                    _serviceInstance.InterfaceMethods.TryAdd(currentMethodIdent, mi);
                    var parameterInfos = mi.GetParameters();
                    var isByRef = new bool[parameterInfos.Length];
                    for (int i = 0; i < isByRef.Length; i++)
                        isByRef[i] = parameterInfos[i].ParameterType.IsByRef;
                    _serviceInstance.MethodParametersByRef.TryAdd(currentMethodIdent, isByRef);
                    var compiled = MethodInvokerCompiler.TryCompile(mi);
                    if (null != compiled) _serviceInstance.CompiledMethods.TryAdd(currentMethodIdent, compiled);
                    currentMethodIdent++;
                }
            }

            var interfaces = _serviceType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                var methodInfos = interfaceType.GetMethods();
                foreach (var mi in methodInfos)
                {
                    _serviceInstance.InterfaceMethods.TryAdd(currentMethodIdent, mi);
                    var parameterInfos = mi.GetParameters();
                    var isByRef = new bool[parameterInfos.Length];
                    for (int i = 0; i < isByRef.Length; i++)
                        isByRef[i] = parameterInfos[i].ParameterType.IsByRef;
                    _serviceInstance.MethodParametersByRef.TryAdd(currentMethodIdent, isByRef);
                    var compiled = MethodInvokerCompiler.TryCompile(mi);
                    if (null != compiled) _serviceInstance.CompiledMethods.TryAdd(currentMethodIdent, compiled);
                    currentMethodIdent++;
                }
            }

            //Create a list of sync infos from the dictionary
            var syncSyncInfos = new List<MethodSyncInfo>();
            foreach (var kvp in _serviceInstance.InterfaceMethods)
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
                ServiceKeyIndex = 0,
                CompressionThreshold = 131072,
                UseCompression = false,
                MethodInfos = syncSyncInfos.ToArray()
            };
            _serviceInstance.ServiceSyncInfo = serviceSyncInfo;
        }

        protected override object[] InvokeMethod(string metaData, params object[] parameters)
        {
            object[] returnParameters = null;
            Type returnType = null;
            try
            {
                var methodSyncInfo = ResolveMethod(metaData);
                var ident = methodSyncInfo.MethodIdent;

                MethodInfo method;
                if (_serviceInstance.InterfaceMethods.TryGetValue(ident, out method))
                {
                    bool[] isByRef;
                    _serviceInstance.MethodParametersByRef.TryGetValue(ident, out isByRef);

                    returnType = (null == method) ? null : method.ReturnType;

                    //invoke the method
                    try
                    {
                        if (null != _interceptPoint.Cut && null != _interceptPoint.Cut.PreInvoke)
                        {
                            _interceptPoint.Cut.PreInvoke(_interceptPoint.Id, methodSyncInfo.MethodName, parameters);
                        }

                        Func<object, object[], object> invoker;
                        object returnValue = (null != _serviceInstance.CompiledMethods
                                && _serviceInstance.CompiledMethods.TryGetValue(ident, out invoker))
                            ? invoker(_serviceInstance.SingletonInstance, parameters)
                            : method.Invoke(_serviceInstance.SingletonInstance, parameters);
                        //the result to the client is the return value (null if void) and the input parameters
                        returnParameters = new object[1 + parameters.Length];
                        returnParameters[0] = returnValue;
                        for (int i = 0; i < parameters.Length; i++)
                            returnParameters[i + 1] = isByRef[i] ? parameters[i] : null;
                    }
                    catch (Exception ex)
                    {
                        Exception exceptionOfConcern = ex;
                        if (exceptionOfConcern is TargetInvocationException && null != exceptionOfConcern.InnerException)
                        {
                            exceptionOfConcern = exceptionOfConcern.InnerException;
                        }
                        bool shouldThrow = true;
                        if (null != _interceptPoint.Cut && null != _interceptPoint.Cut.ExceptionHandler)
                        {
                            shouldThrow = _interceptPoint.Cut.ExceptionHandler(_interceptPoint.Id, methodSyncInfo.MethodName, parameters,
                                exceptionOfConcern);
                        }
                        if (shouldThrow)
                        {
                            returnParameters = new object[] { exceptionOfConcern };
                            throw exceptionOfConcern;
                        } else
                        {
                            returnParameters = new object[]
                            {
                              returnType == typeof(void)? null: returnType.GetDefault()
                            };
                        }
                    }
                    finally
                    {
                        if (null != _interceptPoint.Cut && null != _interceptPoint.Cut.PostInvoke)
                        {
                            _interceptPoint.Cut.PostInvoke(_interceptPoint.Id, methodSyncInfo.MethodName, returnParameters);
                        }
                    }
                    return returnParameters;
                }
                throw new Exception(string.Format("Cannot match method '{0}' to its implementation.", methodSyncInfo.MethodName));
            }
            catch
            {
                //log?
                throw;
            }
        }

        private MethodSyncInfo ResolveMethod(string metaData)
        {
            MethodSyncInfo cachedMethod;
            if (_methodCache.TryGetValue(metaData, out cachedMethod))
                return cachedMethod;

            var mdata = metaData.Split('|');
            for (int index = 0; index < _serviceInstance.ServiceSyncInfo.MethodInfos.Length; index++)
            {
                var method = _serviceInstance.ServiceSyncInfo.MethodInfos[index];
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
                    _methodCache.TryAdd(metaData, method);
                    return method;
                }
            }

            throw new Exception(string.Format("Cannot match method '{0}' to its implementation.", mdata[0]));
        }

        protected override void Dispose(bool disposing)
        {
            if (null != _interceptPoint && null != _interceptPoint.Target && _interceptPoint.Target is IDisposable)
            {
                ((IDisposable)_interceptPoint.Target).Dispose();
            }
        }
    }
}
