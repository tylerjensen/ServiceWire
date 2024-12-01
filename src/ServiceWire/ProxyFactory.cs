using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace ServiceWire
{
    public static class ProxyFactory
    {
        private const string PROXY_ASSEMBLY = "ProxyAssembly";
        private const string INVOKE_METHOD = "InvokeMethod";
        private const string PROXY_MODULE = "ProxyModule";
        private const string PROXY = "Proxy";

        //pooled dictionary achieves same or better performance as ThreadStatic without creating as many builders under average load
        private static PooledDictionary<string, ProxyBuilder> _proxies = new PooledDictionary<string, ProxyBuilder>();

        public static TInterface CreateProxy<TInterface>(Type channelType, Type ctorArgType, object channelCtorValue, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            if (!channelType.InheritsFrom(typeof(Channel))) throw new ArgumentException("channelType does not inherit from Channel");
            Type interfaceType = typeof(TInterface);

            //derive unique key for this dynamic assembly by interface, channel and ctor type names
            var proxyName = interfaceType.ToConfigName() + channelType.ToConfigName() + ctorArgType.ToConfigName();

            //get pooled proxy builder
            var localChannelType = channelType;
            var localCtorArgType = ctorArgType;
            ProxyBuilder proxyBuilder = null;
            TInterface proxy = null;
            try
            {
                proxyBuilder = _proxies.Request(proxyName, () => CreateProxyBuilder(proxyName, interfaceType, localChannelType, localCtorArgType));
                proxy = CreateProxy<TInterface>(proxyBuilder, channelCtorValue, serializer, compressor, logger, stats);
            }
            finally
            {
                //return builder to the pool
                if (null != proxyBuilder) _proxies.Release(proxyName, proxyBuilder);
            }
            return proxy;
        }

        private static TInterface CreateProxy<TInterface>(ProxyBuilder proxyBuilder, object channelCtorValue, ISerializer serializer, ICompressor compressor, ILog logger, IStats stats) where TInterface : class
        {
            //create the type and construct an instance
            Type[] ctorArgTypes = new Type[] { typeof(Type), proxyBuilder.CtorType, typeof(ISerializer), typeof(ICompressor), typeof(ILog), typeof(IStats) };
            Type t = proxyBuilder.TypeBuilder.CreateTypeInfo();
            var constructorInfo = t.GetConstructor(ctorArgTypes);
            if (constructorInfo != null)
            {
                TInterface instance = (TInterface)constructorInfo.Invoke(new object[] { typeof(TInterface), channelCtorValue, serializer, compressor, logger, stats });
                return instance;
            }
            return null;
        }

        private static ProxyBuilder CreateProxyBuilder(string proxyName, Type interfaceType, Type channelType, Type ctorArgType)
        {
            AppDomain domain = Thread.GetDomain();
            // create a new assembly for the proxy
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(PROXY_ASSEMBLY), AssemblyBuilderAccess.Run);

            // create a new module for the proxy
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(PROXY_MODULE);

            // Set the class to be public and sealed
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

            // Construct the type builder
            TypeBuilder typeBuilder = moduleBuilder.DefineType(interfaceType.Name + PROXY, typeAttributes, channelType);
            List<Type> allInterfaces = new List<Type>(interfaceType.GetInterfaces());
            allInterfaces.Add(interfaceType);

            //add the interface
            typeBuilder.AddInterfaceImplementation(interfaceType);

            //construct the constructor
            Type[] ctorArgTypes = new Type[] { typeof(Type), ctorArgType, typeof(ISerializer), typeof(ICompressor), typeof(ILog), typeof(IStats) };
            CreateConstructor(channelType, typeBuilder, ctorArgTypes);

            //construct the type maps
            Dictionary<Type, OpCode> ldindOpCodeTypeMap = new Dictionary<Type, OpCode>();
            ldindOpCodeTypeMap.Add(typeof(Boolean), OpCodes.Ldind_I1);
            ldindOpCodeTypeMap.Add(typeof(Byte), OpCodes.Ldind_U1);
            ldindOpCodeTypeMap.Add(typeof(SByte), OpCodes.Ldind_I1);
            ldindOpCodeTypeMap.Add(typeof(Int16), OpCodes.Ldind_I2);
            ldindOpCodeTypeMap.Add(typeof(UInt16), OpCodes.Ldind_U2);
            ldindOpCodeTypeMap.Add(typeof(Int32), OpCodes.Ldind_I4);
            ldindOpCodeTypeMap.Add(typeof(UInt32), OpCodes.Ldind_U4);
            ldindOpCodeTypeMap.Add(typeof(Int64), OpCodes.Ldind_I8);
            ldindOpCodeTypeMap.Add(typeof(UInt64), OpCodes.Ldind_I8);
            ldindOpCodeTypeMap.Add(typeof(Char), OpCodes.Ldind_U2);
            ldindOpCodeTypeMap.Add(typeof(Double), OpCodes.Ldind_R8);
            ldindOpCodeTypeMap.Add(typeof(Single), OpCodes.Ldind_R4);
            Dictionary<Type, OpCode> stindOpCodeTypeMap = new Dictionary<Type, OpCode>();
            stindOpCodeTypeMap.Add(typeof(Boolean), OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(Byte), OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(SByte), OpCodes.Stind_I1);
            stindOpCodeTypeMap.Add(typeof(Int16), OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(UInt16), OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(Int32), OpCodes.Stind_I4);
            stindOpCodeTypeMap.Add(typeof(UInt32), OpCodes.Stind_I4);
            stindOpCodeTypeMap.Add(typeof(Int64), OpCodes.Stind_I8);
            stindOpCodeTypeMap.Add(typeof(UInt64), OpCodes.Stind_I8);
            stindOpCodeTypeMap.Add(typeof(Char), OpCodes.Stind_I2);
            stindOpCodeTypeMap.Add(typeof(Double), OpCodes.Stind_R8);
            stindOpCodeTypeMap.Add(typeof(Single), OpCodes.Stind_R4);

            //construct the method builders from the method infos defined in the interface
            List<MethodInfo> methods = GetAllMethods(allInterfaces);
            foreach (MethodInfo methodInfo in methods)
            {
                MethodBuilder methodBuilder = ConstructMethod(channelType, methodInfo, typeBuilder, ldindOpCodeTypeMap, stindOpCodeTypeMap);
                typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            }

            //create proxy builder
            var result = new ProxyBuilder
            {
                ProxyName = proxyName,
                InterfaceType = interfaceType,
                CtorType = ctorArgType,
                AssemblyBuilder = assemblyBuilder,
                ModuleBuilder = moduleBuilder,
                TypeBuilder = typeBuilder
            };
            return result;
        }

        private static List<MethodInfo> GetAllMethods(List<Type> allInterfaces)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Type interfaceType in allInterfaces)
                methods.AddRange(interfaceType.GetMethods());
            return methods;
        }

        private static void CreateConstructor(Type channelType, TypeBuilder typeBuilder, Type[] ctorArgTypes)
        {
            ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ctorArgTypes);
            ConstructorInfo baseCtor = channelType.GetConstructor(ctorArgTypes);

            ILGenerator ctorIL = ctor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0); //load "this"
            ctorIL.Emit(OpCodes.Ldarg_1); //load serviceType
            ctorIL.Emit(OpCodes.Ldarg_2); //load "endpoint"
            ctorIL.Emit(OpCodes.Ldarg_3); //load "serializer"
            ctorIL.Emit(OpCodes.Ldarg_S, 4); //load "compressor"
            ctorIL.Emit(OpCodes.Ldarg_S, 5); //load "logger"
            ctorIL.Emit(OpCodes.Ldarg_S, 6); //load "stats"
            ctorIL.Emit(OpCodes.Call, baseCtor); //call "base(...)"
            ctorIL.Emit(OpCodes.Ret);
        }

        private static MethodBuilder ConstructMethod(Type channelType, MethodInfo methodInfo, TypeBuilder typeBuilder, Dictionary<Type, OpCode> ldindOpCodeTypeMap, Dictionary<Type, OpCode> stindOpCodeTypeMap)
        {
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            int nofParams = paramInfos.Length;
            Type[] parameterTypes = new Type[nofParams];
            for (int i = 0; i < nofParams; i++) parameterTypes[i] = paramInfos[i].ParameterType;
            Type returnType = methodInfo.ReturnType;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, returnType, parameterTypes);

            ILGenerator mIL = methodBuilder.GetILGenerator();
            GenerateILCodeForMethod(channelType, methodInfo, mIL, parameterTypes, methodBuilder.ReturnType, ldindOpCodeTypeMap, stindOpCodeTypeMap);
            return methodBuilder;
        }

        private static void GenerateILCodeForMethod(Type channelType, MethodInfo methodInfo, ILGenerator mIL, Type[] inputArgTypes, Type returnType, Dictionary<Type, OpCode> ldindOpCodeTypeMap, Dictionary<Type, OpCode> stindOpCodeTypeMap)
        {
            mIL.Emit(OpCodes.Ldarg_0); //load "this"

            int nofArgs = inputArgTypes.Length;
            //get the MethodInfo for InvokeMethod
            MethodInfo invokeMethodMI = channelType.GetMethod(INVOKE_METHOD, BindingFlags.Instance | BindingFlags.NonPublic);

            //declare local variables
            LocalBuilder resultLB = mIL.DeclareLocal(typeof(object[])); // object[] result

            //set local value with method name and arg types to improve perfmance
            //metadata: methodInfo.Name | inputTypes[x].FullName .. |
            var metadata = methodInfo.Name;
            if (inputArgTypes.Length > 0)
            {
                var args = new string[inputArgTypes.Length];
                for (int i = 0; i < inputArgTypes.Length; i++) args[i] = inputArgTypes[i].ToConfigName();
                metadata += "|" + string.Join("|", args);
            }
            //declare and assign string literal
            LocalBuilder metaLB = mIL.DeclareLocal(typeof(string));
            //mIL.Emit(OpCodes.Dup);  //causes InvalidProgramException - Common Language Runtime detected an invalid program.
            mIL.Emit(OpCodes.Ldstr, metadata);
            mIL.Emit(OpCodes.Stloc_1); //load into metaData local variable

            //load metadata into first param for invokeMethodMI
            //mIL.Emit(OpCodes.Dup);  //causes InvalidProgramException - Common Language Runtime detected an invalid program.
            mIL.Emit(OpCodes.Ldloc_1);

            mIL.Emit(OpCodes.Ldc_I4, nofArgs); //push the number of arguments
            mIL.Emit(OpCodes.Newarr, typeof(object)); //create an array of objects

            //store every input argument in the args array
            for (int i = 0; i < nofArgs; i++)
            {
                Type inputType = inputArgTypes[i].IsByRef ? inputArgTypes[i].GetElementType() : inputArgTypes[i];

                mIL.Emit(OpCodes.Dup);
                mIL.Emit(OpCodes.Ldc_I4, i); //push the index onto the stack
                mIL.Emit(OpCodes.Ldarg, i + 1); //load the i'th argument. This might be an address
                if (inputArgTypes[i].IsByRef)
                {
                    if (inputType.IsValueType)
                    {
                        if (inputType.IsPrimitive)
                        {
                            mIL.Emit(ldindOpCodeTypeMap[inputType]);
                            mIL.Emit(OpCodes.Box, inputType);
                        } else if (inputType.IsEnum && ldindOpCodeTypeMap.ContainsKey(Enum.GetUnderlyingType(inputType)))
                        {
                            //enum with a supported underlying type is converted to that underlying type
                            Type underlyingTypeEnum = Enum.GetUnderlyingType(inputType);
                            mIL.Emit(ldindOpCodeTypeMap[underlyingTypeEnum]);
                            mIL.Emit(OpCodes.Box, inputType);
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
                    if (inputArgTypes[i].IsValueType)
                        mIL.Emit(OpCodes.Box, inputArgTypes[i]);
                }
                mIL.Emit(OpCodes.Stelem_Ref); //store the reference in the args array
            }
            mIL.Emit(OpCodes.Call, invokeMethodMI);
            mIL.Emit(OpCodes.Stloc, resultLB.LocalIndex); //store the result
            //store the results in the arguments
            for (int i = 0; i < nofArgs; i++)
            {
                if (inputArgTypes[i].IsByRef)
                {
                    Type inputType = inputArgTypes[i].GetElementType();
                    mIL.Emit(OpCodes.Ldarg, i + 1); //load the address of the argument
                    mIL.Emit(OpCodes.Ldloc, resultLB.LocalIndex); //load the result array
                    mIL.Emit(OpCodes.Ldc_I4, i + 1); //load the index into the result array
                    mIL.Emit(OpCodes.Ldelem_Ref); //load the value in the index of the array
                    if (inputType.IsEnum && ldindOpCodeTypeMap.ContainsKey(Enum.GetUnderlyingType(inputType)))
                    {
                        //enum with a supported underlying type is converted to that underlying type
                        Type underlyingTypeEnum = Enum.GetUnderlyingType(inputType);
                        mIL.Emit(OpCodes.Unbox, underlyingTypeEnum);
                        mIL.Emit(ldindOpCodeTypeMap[underlyingTypeEnum]);
                        mIL.Emit(stindOpCodeTypeMap[underlyingTypeEnum]);
                    } else if (inputType.IsValueType)
                    {
                        mIL.Emit(OpCodes.Unbox, inputArgTypes[i].GetElementType());
                        mIL.Emit(ldindOpCodeTypeMap[inputArgTypes[i].GetElementType()]);
                        mIL.Emit(stindOpCodeTypeMap[inputArgTypes[i].GetElementType()]);
                    } else
                    {
                        mIL.Emit(OpCodes.Castclass, inputArgTypes[i].GetElementType());
                        mIL.Emit(OpCodes.Stind_Ref); //store the unboxed value at the argument address
                    }
                }
            }
            if (returnType != typeof(void))
            {
                mIL.Emit(OpCodes.Ldloc, resultLB.LocalIndex); //load the result array
                mIL.Emit(OpCodes.Ldc_I4, 0); //load the index of the return value. Alway 0
                mIL.Emit(OpCodes.Ldelem_Ref); //load the value in the index of the array

                if (returnType.IsValueType)
                {
                    mIL.Emit(OpCodes.Unbox, returnType); //unbox it
                    if (returnType.IsPrimitive)          //deal with primitive vs struct value types
                        mIL.Emit(ldindOpCodeTypeMap[returnType]);
                    else
                        mIL.Emit(OpCodes.Ldobj, returnType);
                } else
                {
                    mIL.Emit(OpCodes.Castclass, returnType);
                }
            }
            mIL.Emit(OpCodes.Ret);
        }
    }
}
