using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace ServiceWire
{

    public static class TypeExtensions
    {
        public static Type BaseType(this Type t)
        {
#if NETSTANDARD1_6
            return t.GetTypeInfo().BaseType;
#else
            return t.BaseType;
#endif
        }

        public static bool IsInterface(this Type t)
        {
#if NETSTANDARD1_6
            return t.GetTypeInfo().IsInterface;
#else
            return t.IsInterface;
#endif
        }

#if NETSTANDARD1_6
        public static MethodInfo[] GetMethods(this Type t)
        {
            return t.GetTypeInfo().GetMethods();
        }
#endif

#if NETSTANDARD1_6
        public static Type[] GetInterfaces(this Type t)
        {
            return t.GetTypeInfo().GetInterfaces();
        }
#endif

#if NETSTANDARD1_6
        public static Type CreateType(this TypeBuilder b)
        {
            return b.CreateTypeInfo().AsType();
        }
#endif

#if NETSTANDARD1_6
        public static ConstructorInfo GetConstructor(this Type t, Type[] ctorArgTypes)
        {
            return t.GetTypeInfo().GetConstructor(ctorArgTypes);
        }
#endif

#if NETSTANDARD1_6
        public static MethodInfo GetMethod(this Type t, string invokeMethod, BindingFlags flags)
        {
            return t.GetTypeInfo().GetMethod(invokeMethod, flags);
        }

        public static MethodInfo[] GetMethods(this Type t, BindingFlags flags)
        {
            return t.GetTypeInfo().GetMethods(flags);
        }

        public static PropertyInfo[] GetProperties(this Type t, BindingFlags flags)
        {
            return t.GetTypeInfo().GetProperties(flags);
        }

#endif

        public static bool IsValueType(this Type t)
        {
#if NETSTANDARD1_6
            return t.GetTypeInfo().IsValueType;
#else
            return t.IsValueType;
#endif
        }

        public static Type[] GetGenericArguments(this PropertyInfo pi)
        {
#if NETSTANDARD1_6
            return pi.PropertyType.GetTypeInfo().GetGenericArguments();
#else
            return pi.PropertyType.GetGenericArguments();
#endif
        }

        public static bool IsGenericType(this Type t)
        {
#if NETSTANDARD1_6
            return t.GetTypeInfo().IsGenericType;
#else
            return t.IsGenericType;
#endif
        }

        public static bool IsPrimitive(this Type t)
        {
#if NETSTANDARD1_6
            return t.GetTypeInfo().IsPrimitive;
#else
            return t.IsPrimitive;
#endif
        }

    }

}
