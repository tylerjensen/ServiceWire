#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

#endregion


namespace ServiceWire
{
    public static class ProxyFactory
    {
        #region Fields

        //pooled dictionary achieves same or better performance as ThreadStatic without creating as many builders under average load
        private static readonly PooledDictionary<string,ProxyBuilder> _proxies=new PooledDictionary<string,ProxyBuilder>();

        #endregion


        #region Methods


        #region Public Methods

        public static TInterface CreateProxy<TInterface>(Type channelType,Type ctorArgType,object channelCtorValue) where TInterface : class
        {
            if(!channelType.InheritsFrom(typeof(Channel)))
            {
                throw new ArgumentException("channelType does not inherit from Channel");
            }
            var interfaceType=typeof(TInterface);

            //derive unique key for this dynamic assembly by interface, channel and ctor type names
            var proxyName=interfaceType.FullName+channelType.FullName+ctorArgType.FullName;

            //get pooled proxy builder
            var localChannelType=channelType;
            var localCtorArgType=ctorArgType;
            ProxyBuilder proxyBuilder=null;
            TInterface proxy=null;
            try
            {
                proxyBuilder=_proxies.Request(proxyName,() => CreateProxyBuilder(proxyName,interfaceType,localChannelType,localCtorArgType));
                proxy=CreateProxy<TInterface>(proxyBuilder,channelCtorValue);
            }
            finally
            {
                //return builder to the pool
                if(null!=proxyBuilder)
                {
                    _proxies.Release(proxyName,proxyBuilder);
                }
            }
            return proxy;
        }

        #endregion


        #region Private Methods

        private static TInterface CreateProxy<TInterface>(ProxyBuilder proxyBuilder,object channelCtorValue) where TInterface : class
        {
            //create the type and construct an instance
            Type[] ctorArgTypes={typeof(Type),proxyBuilder.CtorType};
            var t=proxyBuilder.TypeBuilder.CreateType();
            var constructorInfo=t.GetConstructor(ctorArgTypes);
            if(constructorInfo!=null)
            {
                var instance=(TInterface)constructorInfo.Invoke(new[] {typeof(TInterface),channelCtorValue});
                return instance;
            }
            return null;
        }

        private static ProxyBuilder CreateProxyBuilder(string proxyName,Type interfaceType,Type channelType,Type ctorArgType)
        {
            var domain=Thread.GetDomain();

            // create a new assembly for the proxy
            var assemblyBuilder=domain.DefineDynamicAssembly(new AssemblyName(PROXY_ASSEMBLY),AssemblyBuilderAccess.Run);

            // create a new module for the proxy
            var moduleBuilder=assemblyBuilder.DefineDynamicModule(PROXY_MODULE,true);

            // Set the class to be public and sealed
            var typeAttributes=TypeAttributes.Class|TypeAttributes.Public|TypeAttributes.Sealed;

            // Construct the type builder
            var typeBuilder=moduleBuilder.DefineType(interfaceType.Name+PROXY,typeAttributes,channelType);
            var allInterfaces=new List<Type>(interfaceType.GetInterfaces());
            allInterfaces.Add(interfaceType);

            //add the interface
            typeBuilder.AddInterfaceImplementation(interfaceType);

            //construct the constructor
            Type[] ctorArgTypes={typeof(Type),ctorArgType};
            CreateConstructor(channelType,typeBuilder,ctorArgTypes);

            //construct the type maps
            var ldindOpCodeTypeMap=new Dictionary<Type,OpCode>();
            ldindOpCodeTypeMap.Add(typeof(bool),OpCodes.Ldind_I1);
            ldindOpCodeTypeMap.Add(typeof(byte),OpCodes.Ldind_U1);
            ldindOpCodeTypeMap.Add(typeof(sbyte),OpCodes.Ldind_I1);
            ldindOpCodeTypeMap.Add(typeof(short),OpCodes.Ldind_I2);
            ldindOpCodeTypeMap.Add(typeof(ushort),OpCodes.Ldind_U2);
            ldindOpCodeTypeMap.Add(typeof(int),OpCodes.Ldind_I4);
            ldindOpCodeTypeMap.Add(typeof(uint),OpCodes.Ldind_U4);
            ldindOpCodeTypeMap.Add(typeof(long),OpCodes.Ldind_I8);
            ldindOpCodeTypeMap.Add(typeof(ulong),OpCodes.Ldind_I8);
            ldindOpCodeTypeMap.Add(typeof(char),OpCodes.Ldind_U2);
            ldindOpCodeTypeMap.Add(typeof(double),OpCodes.Ldind_R8);
            ldindOpCodeTypeMap.Add(typeof(float),OpCodes.Ldind_R4);
            var stindOpCodeTypeMap=new Dictionary<Type,OpCode>();
            stindOpCodeTypeMap.Add(typeof(bool),OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(byte),OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(sbyte),OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(short),OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(ushort),OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(int),OpCodes.Stind_I4);
            stindOpCodeTypeMap.Add(typeof(uint),OpCodes.Stind_I4);
            stindOpCodeTypeMap.Add(typeof(long),OpCodes.Stind_I8);
            stindOpCodeTypeMap.Add(typeof(ulong),OpCodes.Stind_I8);
            stindOpCodeTypeMap.Add(typeof(char),OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(double),OpCodes.Stind_R8);
            stindOpCodeTypeMap.Add(typeof(float),OpCodes.Stind_R4);

            //construct the method builders from the method infos defined in the interface
            var methods=GetAllMethods(allInterfaces);
            foreach(var methodInfo in methods)
            {
                var methodBuilder=ConstructMethod(channelType,methodInfo,typeBuilder,ldindOpCodeTypeMap,stindOpCodeTypeMap);
                typeBuilder.DefineMethodOverride(methodBuilder,methodInfo);
            }

            //create proxy builder
            var result=new ProxyBuilder {ProxyName=proxyName,InterfaceType=interfaceType,CtorType=ctorArgType,AssemblyBuilder=assemblyBuilder,ModuleBuilder=moduleBuilder,TypeBuilder=typeBuilder};
            return result;
        }

        private static List<MethodInfo> GetAllMethods(List<Type> allInterfaces)
        {
            var methods=new List<MethodInfo>();
            foreach(var interfaceType in allInterfaces)
            {
                methods.AddRange(interfaceType.GetMethods());
            }
            return methods;
        }

        private static void CreateConstructor(Type channelType,TypeBuilder typeBuilder,Type[] ctorArgTypes)
        {
            var ctor=typeBuilder.DefineConstructor(MethodAttributes.Public,CallingConventions.HasThis,ctorArgTypes);
            var baseCtor=channelType.GetConstructor(ctorArgTypes);

            var ctorIL=ctor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0); //load "this"
            ctorIL.Emit(OpCodes.Ldarg_1); //load serviceType
            ctorIL.Emit(OpCodes.Ldarg_2); //load "endpoint"
            ctorIL.Emit(OpCodes.Call,baseCtor); //call "base(...)"
            ctorIL.Emit(OpCodes.Ret);
        }

        private static MethodBuilder ConstructMethod(Type channelType,MethodInfo methodInfo,TypeBuilder typeBuilder,Dictionary<Type,OpCode> ldindOpCodeTypeMap,Dictionary<Type,OpCode> stindOpCodeTypeMap)
        {
            var paramInfos=methodInfo.GetParameters();
            var nofParams=paramInfos.Length;
            var parameterTypes=new Type[nofParams];
            for(var i=0;i<nofParams;i++)
            {
                parameterTypes[i]=paramInfos[i].ParameterType;
            }
            var returnType=methodInfo.ReturnType;
            var methodBuilder=typeBuilder.DefineMethod(methodInfo.Name,MethodAttributes.Public|MethodAttributes.Virtual,returnType,parameterTypes);

            var mIL=methodBuilder.GetILGenerator();
            GenerateILCodeForMethod(channelType,methodInfo,mIL,parameterTypes,methodBuilder.ReturnType,ldindOpCodeTypeMap,stindOpCodeTypeMap);
            return methodBuilder;
        }

        private static void GenerateILCodeForMethod(Type channelType,MethodInfo methodInfo,ILGenerator mIL,Type[] inputArgTypes,Type returnType,Dictionary<Type,OpCode> ldindOpCodeTypeMap,Dictionary<Type,OpCode> stindOpCodeTypeMap)
        {
            mIL.Emit(OpCodes.Ldarg_0); //load "this"

            var nofArgs=inputArgTypes.Length;

            //get the MethodInfo for InvokeMethod
            var invokeMethodMI=channelType.GetMethod(INVOKE_METHOD,BindingFlags.Instance|BindingFlags.NonPublic);

            //declare local variables
            var resultLB=mIL.DeclareLocal(typeof(object[])); // object[] result

            //set local value with method name and arg types to improve perfmance
            //metadata: methodInfo.Name | inputTypes[x].FullName .. |
            var metadata=methodInfo.Name;
            if(inputArgTypes.Length>0)
            {
                var args=new string[inputArgTypes.Length];
                for(var i=0;i<inputArgTypes.Length;i++)
                {
                    args[i]=inputArgTypes[i].FullName;
                }
                metadata+="|"+string.Join("|",args);
            }

            //declare and assign string literal
            var metaLB=mIL.DeclareLocal(typeof(string));
            metaLB.SetLocalSymInfo("metaData",1,2);
            mIL.Emit(OpCodes.Dup);
            mIL.Emit(OpCodes.Ldstr,metadata);
            mIL.Emit(OpCodes.Stloc_1); //load into metaData local variable

            //load metadata into first param for invokeMethodMI
            mIL.Emit(OpCodes.Dup);
            mIL.Emit(OpCodes.Ldloc_1);

            mIL.Emit(OpCodes.Ldc_I4,nofArgs); //push the number of arguments
            mIL.Emit(OpCodes.Newarr,typeof(object)); //create an array of objects

            //store every input argument in the args array
            for(var i=0;i<nofArgs;i++)
            {
                var inputType=inputArgTypes[i].IsByRef ? inputArgTypes[i].GetElementType() : inputArgTypes[i];

                mIL.Emit(OpCodes.Dup);
                mIL.Emit(OpCodes.Ldc_I4,i); //push the index onto the stack
                mIL.Emit(OpCodes.Ldarg,i+1); //load the i'th argument. This might be an address			
                if(inputArgTypes[i].IsByRef)
                {
                    if(inputType.IsValueType)
                    {
                        if(inputType.IsPrimitive)
                        {
                            mIL.Emit(ldindOpCodeTypeMap[inputType]);
                            mIL.Emit(OpCodes.Box,inputType);
                        } else
                        {
                            throw new NotSupportedException("Non-primitive native types (e.g. Decimal and Guid) ByRef are not supported.");
                        }
                    } else
                    {
                        mIL.Emit(OpCodes.Ldind_Ref);
                    }
                } else
                {
                    if(inputArgTypes[i].IsValueType)
                    {
                        mIL.Emit(OpCodes.Box,inputArgTypes[i]);
                    }
                }
                mIL.Emit(OpCodes.Stelem_Ref); //store the reference in the args array
            }
            mIL.Emit(OpCodes.Call,invokeMethodMI);
            mIL.Emit(OpCodes.Stloc,resultLB.LocalIndex); //store the result

            //store the results in the arguments
            for(var i=0;i<nofArgs;i++)
            {
                if(inputArgTypes[i].IsByRef)
                {
                    var inputType=inputArgTypes[i].GetElementType();
                    mIL.Emit(OpCodes.Ldarg,i+1); //load the address of the argument
                    mIL.Emit(OpCodes.Ldloc,resultLB.LocalIndex); //load the result array
                    mIL.Emit(OpCodes.Ldc_I4,i+1); //load the index into the result array
                    mIL.Emit(OpCodes.Ldelem_Ref); //load the value in the index of the array
                    if(inputType.IsValueType)
                    {
                        mIL.Emit(OpCodes.Unbox,inputArgTypes[i].GetElementType());
                        mIL.Emit(ldindOpCodeTypeMap[inputArgTypes[i].GetElementType()]);
                        mIL.Emit(stindOpCodeTypeMap[inputArgTypes[i].GetElementType()]);
                    } else
                    {
                        mIL.Emit(OpCodes.Castclass,inputArgTypes[i].GetElementType());
                        mIL.Emit(OpCodes.Stind_Ref); //store the unboxed value at the argument address
                    }
                }
            }
            if(returnType!=typeof(void))
            {
                mIL.Emit(OpCodes.Ldloc,resultLB.LocalIndex); //load the result array
                mIL.Emit(OpCodes.Ldc_I4,0); //load the index of the return value. Alway 0
                mIL.Emit(OpCodes.Ldelem_Ref); //load the value in the index of the array

                if(returnType.IsValueType)
                {
                    mIL.Emit(OpCodes.Unbox,returnType); //unbox it
                    if(returnType.IsPrimitive) //deal with primitive vs struct value types
                    {
                        mIL.Emit(ldindOpCodeTypeMap[returnType]);
                    } else
                    {
                        mIL.Emit(OpCodes.Ldobj,returnType);
                    }
                } else
                {
                    mIL.Emit(OpCodes.Castclass,returnType);
                }
            }
            mIL.Emit(OpCodes.Ret);
        }

        #endregion


        #endregion


        #region  Others

        private const string PROXY_ASSEMBLY="ProxyAssembly";
        private const string INVOKE_METHOD="InvokeMethod";
        private const string PROXY_MODULE="ProxyModule";
        private const string PROXY="Proxy";

        #endregion
    }
}