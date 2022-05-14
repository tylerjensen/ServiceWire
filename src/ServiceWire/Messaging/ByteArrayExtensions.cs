using System;
using System.Security.Cryptography;
using System.Text;

namespace ServiceWire.Messaging
{
    public static class ByteArrayExtensions
    {
        public static byte[] ConvertToBytes(this string val)
        {
            return Encoding.UTF8.GetBytes(val);
        }

        public static string ConvertToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static bool IsEqualTo(this byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }

        public static byte[] ToBytes(this RSAParameters p)
        {
            var parts = new string[]
            {
                null == p.D ? string.Empty : Convert.ToBase64String(p.D),
                null == p.DP ? string.Empty : Convert.ToBase64String(p.DP),
                null == p.DQ ? string.Empty : Convert.ToBase64String(p.DQ),
                null == p.Exponent ? string.Empty : Convert.ToBase64String(p.Exponent),
                null == p.InverseQ ? string.Empty : Convert.ToBase64String(p.InverseQ),
                null == p.Modulus ? string.Empty : Convert.ToBase64String(p.Modulus),
                null == p.P ? string.Empty : Convert.ToBase64String(p.P),
                null == p.Q ? string.Empty : Convert.ToBase64String(p.Q)
            };
            var data = Encoding.UTF8.GetBytes(string.Join(",", parts));
            return data;
        }

        public static RSAParameters ToRSAParameters(this byte[] data)
        {
            try
            {
                var paramString = Encoding.UTF8.GetString(data);
                var parts = paramString.Split(',');
                if (parts.Length != 8) return default(RSAParameters);
                var result = new RSAParameters();
                result.D = string.Empty != parts[0] ? Convert.FromBase64String(parts[0]) : null;
                result.DP = string.Empty != parts[1] ? Convert.FromBase64String(parts[1]) : null;
                result.DQ = string.Empty != parts[2] ? Convert.FromBase64String(parts[2]) : null;
                result.Exponent = string.Empty != parts[3] ? Convert.FromBase64String(parts[3]) : null;
                result.InverseQ = string.Empty != parts[4] ? Convert.FromBase64String(parts[4]) : null;
                result.Modulus = string.Empty != parts[5] ? Convert.FromBase64String(parts[5]) : null;
                result.P = string.Empty != parts[6] ? Convert.FromBase64String(parts[6]) : null;
                result.Q = string.Empty != parts[7] ? Convert.FromBase64String(parts[7]) : null;
                return result;
            }
            catch
            {
                return default(RSAParameters);
            }
        }
    }
}