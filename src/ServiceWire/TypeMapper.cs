﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class TypeMapper
    {
        private static readonly ConcurrentDictionary<string, Type> mappedTypes;

        static TypeMapper()
        {
            mappedTypes = new ConcurrentDictionary<string, Type>();

            Assembly[] assemblies = new[] { typeof(string).Assembly, typeof(Uri).Assembly };

            foreach(Type type in assemblies.SelectMany(assem => assem.GetTypes().Where(x => x.Namespace?.Equals("System", StringComparison.OrdinalIgnoreCase) ?? default)))
            {
                mappedTypes.TryAdd(type.FullName, type);
            }
        }

        public static Type GetType(string fullTypeName)
        {
            bool isArrayType = fullTypeName.EndsWith("[]");

            fullTypeName = fullTypeName.TrimEnd("[]".ToCharArray());

            if (mappedTypes.TryGetValue(fullTypeName, out Type type))
            {
                return isArrayType ? type.MakeArrayType() : type;
            }
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ServiceWireResources.TypeIsNotMapped, fullTypeName));
        }

        public static async Task<Type> GetTypeAsync(string fullTypeName)
        {
            return await Task.Run(() => GetType(fullTypeName));
        }
    }
}
