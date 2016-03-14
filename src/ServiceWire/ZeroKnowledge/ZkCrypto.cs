#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Security.Cryptography;

#endregion


namespace ServiceWire.ZeroKnowledge
{
    /// <summary>
    ///     Easy to use encapsulation of Rijndael encryption.
    /// </summary>
    public class ZkCrypto
    {
        #region Constractor

        public ZkCrypto(byte[] key,byte[] iv)
        {
            if(key.Length!=32)
            {
                throw new ArgumentException("key must be 256 bits","key");
            }
            if(iv.Length!=32)
            {
                throw new ArgumentException("iv must be 256 bits","iv");
            }
            _key=key;
            _iv=iv;
        }

        #endregion


        #region Fields

        private readonly byte[] _key;
        private readonly byte[] _iv;

        #endregion


        #region Methods


        #region Public Methods

        public byte[] Encrypt(byte[] data)
        {
            using(var crypto=Rijndael.Create())
            {
                crypto.Mode=CipherMode.CBC;
                crypto.BlockSize=256;
                crypto.KeySize=256;
                crypto.Padding=PaddingMode.ISO10126;
                using(var encryptor=crypto.CreateEncryptor(_key,_iv))
                {
                    return encryptor.TransformFinalBlock(data,0,data.Length);
                }
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            using(var crypto=Rijndael.Create())
            {
                crypto.Mode=CipherMode.CBC;
                crypto.BlockSize=256;
                crypto.KeySize=256;
                crypto.Padding=PaddingMode.ISO10126;
                using(var dencryptor=crypto.CreateDecryptor(_key,_iv))
                {
                    return dencryptor.TransformFinalBlock(encrypted,0,encrypted.Length);
                }
            }
        }

        #endregion


        #endregion
    }
}