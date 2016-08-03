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

        public ZkCrypto(byte[] key, byte[] iv)
        {
            if (key.Length != 32) throw new ArgumentException("key must be 256 bits", "key");
            if (iv.Length != 32) throw new ArgumentException("iv must be 256 bits", "iv");
            _key = key;
            _iv = iv;
        }

        public byte[] Encrypt(byte[] data)
        {
            using (var crypto = Aes.Create())
            {
                crypto.Mode = CipherMode.CBC;
                crypto.BlockSize = 256;
                crypto.KeySize = 256;
                crypto.Padding = PaddingMode.PKCS7;
                using (var encryptor = crypto.CreateEncryptor(_key, _iv))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            using (var crypto = Aes.Create())
            {
                crypto.Mode = CipherMode.CBC;
                crypto.BlockSize = 256;
                crypto.KeySize = 256;
                crypto.Padding = PaddingMode.PKCS7;
                using (var dencryptor = crypto.CreateDecryptor(_key, _iv))
                {
                    return dencryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }
        }

    }
}