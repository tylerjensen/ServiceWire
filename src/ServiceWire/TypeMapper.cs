// 
// TypeMapper.cs
//  
// Author:
//       Kenneth Carter <kccarter32@gmail.com>
// 
// Copyright (c) 2020 SubSonic-Core. (https://github.com/SubSonic-Core)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
