#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:35

#endregion


#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

#endregion


namespace ServiceWire.Aspects
{
    public class InterceptChannel:Channel
    {
        #region Constractor

        public InterceptChannel(Type interceptedType,InterceptPoint interceptPoint)
        {
            _serviceType=interceptedType;
            InterceptPoint=interceptPoint;
            CreateMethodMap();
        }

        #endregion


        #region Fields

        private ServiceInstance _serviceInstance;

        #endregion


        #region  Proporties

        public InterceptPoint InterceptPoint { get; }

        #endregion


        #region Methods


        #region Private Methods

        /// <summary>
        ///     Loads all methods from interfaces and assigns an identifier
        ///     to each. These are later synchronized with the client.
        /// </summary>
        private void CreateMethodMap()
        {
            _serviceInstance=new ServiceInstance {KeyIndex=0, //only one per intercepted interface
                InterfaceType=_serviceType,InterfaceMethods=new ConcurrentDictionary<int,MethodInfo>(),MethodParametersByRef=new ConcurrentDictionary<int,bool[]>(),SingletonInstance=InterceptPoint.Target};

            var currentMethodIdent=0;
            if(_serviceType.IsInterface)
            {
                var methodInfos=_serviceType.GetMethods();
                foreach(var mi in methodInfos)
                {
                    _serviceInstance.InterfaceMethods.TryAdd(currentMethodIdent,mi);
                    var parameterInfos=mi.GetParameters();
                    var isByRef=new bool[parameterInfos.Length];
                    for(var i=0;i<isByRef.Length;i++)
                    {
                        isByRef[i]=parameterInfos[i].ParameterType.IsByRef;
                    }
                    _serviceInstance.MethodParametersByRef.TryAdd(currentMethodIdent,isByRef);
                    currentMethodIdent++;
                }
            }

            var interfaces=_serviceType.GetInterfaces();
            foreach(var interfaceType in interfaces)
            {
                var methodInfos=interfaceType.GetMethods();
                foreach(var mi in methodInfos)
                {
                    _serviceInstance.InterfaceMethods.TryAdd(currentMethodIdent,mi);
                    var parameterInfos=mi.GetParameters();
                    var isByRef=new bool[parameterInfos.Length];
                    for(var i=0;i<isByRef.Length;i++)
                    {
                        isByRef[i]=parameterInfos[i].ParameterType.IsByRef;
                    }
                    _serviceInstance.MethodParametersByRef.TryAdd(currentMethodIdent,isByRef);
                    currentMethodIdent++;
                }
            }


            //Create a list of sync infos from the dictionary
            var syncSyncInfos=new List<MethodSyncInfo>();
            foreach(var kvp in _serviceInstance.InterfaceMethods)
            {
                var parameters=kvp.Value.GetParameters();
                var parameterTypes=new Type[parameters.Length];
                for(var i=0;i<parameters.Length;i++)
                {
                    parameterTypes[i]=parameters[i].ParameterType;
                }
                syncSyncInfos.Add(new MethodSyncInfo {MethodIdent=kvp.Key,MethodName=kvp.Value.Name,ParameterTypes=parameterTypes});
            }

            var serviceSyncInfo=new ServiceSyncInfo {ServiceKeyIndex=0,CompressionThreshold=131072,UseCompression=false,MethodInfos=syncSyncInfos.ToArray()};
            _serviceInstance.ServiceSyncInfo=serviceSyncInfo;
        }

        #endregion


        #region Protected Methods

        protected override void SyncInterface(Type interceptedType,string username=null,string password=null)
        {
            //do nothing in this channel
        }

        protected override object[] InvokeMethod(string metaData,params object[] parameters)
        {
            object[] returnParameters=null;
            Type returnType=null;
            try
            {
                var mdata=metaData.Split('|');
                var ident=-1;
                for(var index=0;index<_serviceInstance.ServiceSyncInfo.MethodInfos.Length;index++)
                {
                    var si=_serviceInstance.ServiceSyncInfo.MethodInfos[index];

                    //first of all the method names must match
                    if(si.MethodName==mdata[0])
                    {
                        //second of all the parameter types and -count must match
                        if(mdata.Length-1==si.ParameterTypes.Length)
                        {
                            var matchingParameterTypes=true;
                            for(var i=0;i<si.ParameterTypes.Length;i++)
                            {
                                if(!mdata[i+1].Equals(si.ParameterTypes[i].FullName))
                                {
                                    matchingParameterTypes=false;
                                    break;
                                }
                            }
                            if(matchingParameterTypes)
                            {
                                ident=si.MethodIdent;
                                break;
                            }
                        }
                    }
                }

                if(ident<0)
                {
                    throw new Exception(string.Format("Cannot match method '{0}' to its implementation.",mdata[0]));
                }

                if(_serviceInstance.InterfaceMethods.ContainsKey(ident))
                {
                    MethodInfo method;
                    _serviceInstance.InterfaceMethods.TryGetValue(ident,out method);

                    bool[] isByRef;
                    _serviceInstance.MethodParametersByRef.TryGetValue(ident,out isByRef);

                    returnType=(null==method) ? null : method.ReturnType;

                    //invoke the method
                    var returnMessageType=MessageType.ReturnValues;
                    try
                    {
                        if(null!=InterceptPoint.Cut&&null!=InterceptPoint.Cut.PreInvoke)
                        {
                            InterceptPoint.Cut.PreInvoke(InterceptPoint.Id,mdata[0],parameters);
                        }

                        var returnValue=method.Invoke(_serviceInstance.SingletonInstance,parameters);

                        //the result to the client is the return value (null if void) and the input parameters
                        returnParameters=new object[1+parameters.Length];
                        returnParameters[0]=returnValue;
                        for(var i=0;i<parameters.Length;i++)
                        {
                            returnParameters[i+1]=isByRef[i] ? parameters[i] : null;
                        }
                    }
                    catch(Exception ex)
                    {
                        var exceptionOfConcern=ex;
                        if(exceptionOfConcern is TargetInvocationException&&null!=exceptionOfConcern.InnerException)
                        {
                            exceptionOfConcern=exceptionOfConcern.InnerException;
                        }
                        var shouldThrow=true;
                        if(null!=InterceptPoint.Cut&&null!=InterceptPoint.Cut.ExceptionHandler)
                        {
                            shouldThrow=InterceptPoint.Cut.ExceptionHandler(InterceptPoint.Id,mdata[0],parameters,exceptionOfConcern);
                        }
                        if(shouldThrow)
                        {
                            returnParameters=new object[] {exceptionOfConcern};
                            throw exceptionOfConcern;
                        } else
                        {
                            returnParameters=new[] {returnType.GetDefault()};
                        }
                    }
                    finally
                    {
                        if(null!=InterceptPoint.Cut&&null!=InterceptPoint.Cut.PostInvoke)
                        {
                            InterceptPoint.Cut.PostInvoke(InterceptPoint.Id,mdata[0],returnParameters);
                        }
                    }
                    return returnParameters;
                }
                throw new Exception(string.Format("Cannot match method '{0}' to its implementation.",mdata[0]));
            }
            catch(Exception outerException)
            {
                //log?
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(null!=InterceptPoint&&null!=InterceptPoint.Target&&InterceptPoint.Target is IDisposable)
            {
                ((IDisposable)InterceptPoint.Target).Dispose();
            }
        }

        #endregion


        #endregion
    }
}