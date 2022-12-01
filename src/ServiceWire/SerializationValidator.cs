using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceWire
{
    internal static class SerializationValidator
    {
        public static List<string> NonSerializableTypes(this Type type)
        {
            var nonSerializableTypes = new List<string>();
            AnalyzeType(type, nonSerializableTypes);
            return nonSerializableTypes;
        }

        public static void ValidateServiceInterface(this Type type)
        {
            var nonSerializableTypes = new List<string>();
            var methods = type.GetMethods(BindingFlags.Public);
            foreach (var method in methods)
            {
                if (method.ReturnType != typeof (void))
                {
                    AnalyzeType(method.ReturnType, nonSerializableTypes);
                }
                var parameters = method.GetParameters();
                foreach (var parameter in parameters)
                {
                    if (parameter.ParameterType.IsByRef)
                    {
                        Type inputType = parameter.ParameterType.GetElementType();
                        if (inputType.IsValueType && !inputType.IsPrimitive)
                            throw new NotSupportedException("Non-primitive native types (e.g. Decimal and Guid) ByRef are not supported.");
                    }
                    AnalyzeType(parameter.ParameterType, nonSerializableTypes);
                }
            }

            if (nonSerializableTypes.Count > 0)
            {
                var errorMessage =
                    string.Format("One or more types in {0} are not marked with the Serializable attribute: {1}",
                        type.ToConfigName(), string.Join(",", nonSerializableTypes));
                throw new SerializationException(errorMessage);
            }
        }

        private static void AnalyzeType(Type type, List<string> nonSerializableTypes)
        {
            if (type.IsValueType || type == typeof(string)) return;
            if (type.IsEnum)
            {
                //enum with a supported underlying type is converted to that underlying type
                Type underlyingTypeEnum = Enum.GetUnderlyingType(type);
                if (underlyingTypeEnum.IsValueType || underlyingTypeEnum == typeof(string)) return;
            }

            if (!IsSerializable(type))
                nonSerializableTypes.Add(type.Name);

            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (propertyInfo.PropertyType.IsGenericType)
                {
                    foreach (var genericArgument in propertyInfo.PropertyType.GetGenericArguments())
                    {
                        if (genericArgument == type) continue; // base case for circularly referenced properties
                        AnalyzeType(genericArgument, nonSerializableTypes);
                    }
                }
                else if (propertyInfo.GetType() != type) // base case for circularly referenced properties
                    AnalyzeType(propertyInfo.PropertyType, nonSerializableTypes);
            }
        }

        private static bool IsSerializable(Type type)
        {
            return (Attribute.IsDefined(type, typeof(SerializableAttribute)));
        }
    }
}
