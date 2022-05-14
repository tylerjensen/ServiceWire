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
        private readonly ILog _logger;

        public ZkCrypto(byte[] key, byte[] iv, ILog logger)
        {
            if (key.Length != 32) throw new ArgumentException("key must be 256 bits", "key");
            if (iv.Length != 16) throw new ArgumentException("iv must be 128 bits", "iv");
            _key = key;
            _iv = iv;
            _logger = logger ?? new NullLogger();
        }

        public byte[] Encrypt(byte[] data)
        {
            try
            {
                using (var crypto = Aes.Create())
                {
                    crypto.Mode = CipherMode.CBC;
                    crypto.BlockSize = 128; // 256;
                    crypto.KeySize = 256;
                    crypto.Padding = PaddingMode.PKCS7;
                    using (var encryptor = crypto.CreateEncryptor(_key, _iv))
                    {
                        return encryptor.TransformFinalBlock(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("Encryption error {0}", e);
                return data;
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            try
            {
                using (var crypto = Aes.Create())
                {
                    crypto.Mode = CipherMode.CBC;
                    crypto.BlockSize = 128; // 256;
                    crypto.KeySize = 256;
                    crypto.Padding = PaddingMode.PKCS7;
                    using (var dencryptor = crypto.CreateDecryptor(_key, _iv))
                    {
                        return dencryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("Decryption error {0}", e);
                return encrypted;
            }
        }

    }
}