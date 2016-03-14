#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System.Text;

#endregion


namespace ServiceWire.ZeroKnowledge
{
    public static class ZkExt
    {
        #region Methods


        #region Public Methods

        public static byte[] ConvertToBytes(this string val)
        {
            return Encoding.Unicode.GetBytes(val);
        }

        public static string ConverToString(this byte[] bytes)
        {
            return Encoding.Unicode.GetString(bytes);
        }

        public static bool IsEqualTo(this byte[] a1,byte[] a2)
        {
            if(a1.Length!=a2.Length)
            {
                return false;
            }
            for(var i=0;i<a1.Length;i++)
            {
                if(a1[i]!=a2[i])
                {
                    return false;
                }
            }
            return true;
        }

        #endregion


        #endregion
    }
}