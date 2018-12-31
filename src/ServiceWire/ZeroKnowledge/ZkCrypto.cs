using System;
using System.Security.Cryptography;

namespace ServiceWire.ZeroKnowledge
{
    /// <summary>
    /// Easy to use encapsulation of Rijndael encryption.
    /// </summary>
    public class ZkCrypto
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly MD5CryptoServiceProvider _md5;

        public ZkCrypto(byte[] key, byte[] iv)
        {
            if (key.Length != 32) throw new ArgumentException("key must be 256 bits", "key");
            if (iv.Length != 32) throw new ArgumentException("iv must be 256 bits", "iv");
            _md5 = new MD5CryptoServiceProvider();
            _key = key;
            _iv = _md5.ComputeHash(iv);
        }

        public byte[] Encrypt(byte[] data)
        {
            using (var crypto = RijndaelManaged.Create())
            {
                crypto.Mode = CipherMode.CBC;
                crypto.BlockSize = 128;
                crypto.KeySize = 256;
                crypto.Padding = PaddingMode.ISO10126;
                using (var encryptor = crypto.CreateEncryptor(_key, _iv))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            using (var crypto = RijndaelManaged.Create())
            {
                crypto.Mode = CipherMode.CBC;
                crypto.BlockSize = 128;
                crypto.KeySize = 256;
                crypto.Padding = PaddingMode.ISO10126;
                using (var dencryptor = crypto.CreateDecryptor(_key, _iv))
                {
                    return dencryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }
        }
    }
}