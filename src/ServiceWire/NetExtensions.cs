#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

#endregion


namespace ServiceWire
{
    public static class NetExtensions
    {
        public static string ToConfigName(this Type pT)
        {
            var mName=pT.AssemblyQualifiedName;
            if(mName==null)
            {
                throw new ArgumentNullException(nameof(mName));
            }
            mName=Regex.Replace(mName,@", Version=\d+.\d+.\d+.\d+",string.Empty);
            mName=Regex.Replace(mName,@", Culture=\w+",string.Empty);
            mName=Regex.Replace(mName,@", PublicKeyToken=\w+",string.Empty);
            return mName;

            //return t.FullName + ", " + t.Assembly.GetName().Name;
        }

        public static Type ToType(this string pConfigName)
        {
            try
            {
                return Type.GetType(pConfigName);

                //var parts = (from n in configName.Split(',') select n.Trim()).ToArray();
                //var assembly = Assembly.Load(new AssemblyName(parts[1]));
                //var type = assembly.GetType(parts[0]);
                //return type;
            }
            catch(Exception)
            {
                // ignored
            }
            return null;
        }

        private static readonly JsonSerializerSettings settings=new JsonSerializerSettings {ReferenceLoopHandling=ReferenceLoopHandling.Ignore};

        public static byte[] ToSerializedBytes<T>(this T obj)
        {
            if(null==obj)
            {
                return null;
            }
            var mJson=JsonConvert.SerializeObject(obj,settings);
            return Encoding.UTF8.GetBytes(mJson);
        }

        public static byte[] ToSerializedBytes(this object pObj,string typeConfigName)
        {
            if(null==pObj)
            {
                return null;
            }
            var type=typeConfigName.ToType();
            var json=JsonConvert.SerializeObject(pObj,type,settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T ToDeserializedObject<T>(this byte[] bytes)
        {
            if(null==bytes||bytes.Length==0)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes),settings);
        }

        public static object ToDeserializedObject(this byte[] bytes,string typeConfigName)
        {
            if(null==typeConfigName||null==bytes||bytes.Length==0)
            {
                return null;
            }
            var mType=typeConfigName.ToType();
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes),mType,settings);
        }

        public static string ToSerializedBase64String<T>(this T obj)
        {
            if(null==obj)
            {
                return null;
            }
            var mBytes=obj.ToSerializedBytes();
            return Convert.ToBase64String(mBytes);
        }

        public static T ToDeserializedObjectFromBase64String<T>(this string base64String)
        {
            try
            {
                var bytes=Convert.FromBase64String(base64String);
                return bytes.ToDeserializedObject<T>();
            }
            catch
            {
            }
            return default(T);
        }

        /// <summary>
        ///     Returns true if Type inherits from baseType.
        /// </summary>
        /// <param name="pT">The Type extended by this method.</param>
        /// <param name="pBaseType">The base type to find in the inheritance hierarchy.</param>
        /// <returns>True if baseType is found. False if not.</returns>
        public static bool InheritsFrom(this Type pT,Type pBaseType)
        {
            var mCur=pT.BaseType;
            while(mCur!=null)
            {
                if(mCur==pBaseType)
                {
                    return true;
                }
                mCur=mCur.BaseType;
            }
            return false;
        }

        public static object GetDefault(this Type t)
        {
            var tm=new DefaultTypeMaker();
            return tm.GetDefault(t);
        }

        public static byte[] ToGZipBytes(this byte[] data)
        {
            using(var mSCompressed=new MemoryStream())
            {
                using(var msObj=new MemoryStream(data))
                {
                    using(var gzs=new GZipStream(mSCompressed,CompressionMode.Compress))
                    {
                        msObj.CopyTo(gzs);
                    }
                }
                return mSCompressed.ToArray();
            }
        }

#if (NET35)
        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
#endif

        public static byte[] FromGZipBytes(this byte[] compressedBytes)
        {
            using(var msObj=new MemoryStream())
            {
                using(var msCompressed=new MemoryStream(compressedBytes))
                {
                    using(var mGzs=new GZipStream(msCompressed,CompressionMode.Decompress))
                    {
                        mGzs.CopyTo(msObj);
                    }
                }
                msObj.Seek(0,SeekOrigin.Begin);
                return msObj.ToArray();
            }
        }

        public static string Flatten(this string src)
        {
            return src.Replace("\r",":").Replace("\n",":"); // Not L10N
        }
    }
}