using System;
using System.Security.Cryptography;

namespace ServiceWire.ZeroKnowledge
{
    /// <summary>
    /// Easy to use encapsulation of Rijndael encryption.
    /// </summary>
    public class ZkCrypto : IDisposable
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly MD5 _md5;
        private readonly SymmetricAlgorithm _crypto;

        //encrypt and decrypt use independent transforms, so each direction only needs
        //to be serialized against itself (writers vs the response reader thread)
        private readonly object _encLock = new object();
        private readonly object _decLock = new object();

        public ZkCrypto(byte[] key, byte[] iv)
        {
            if (key.Length != 32) throw new ArgumentException("key must be 256 bits", "key");
            if (iv.Length != 32) throw new ArgumentException("iv must be 256 bits", "iv");
            _md5 = MD5.Create();
            _key = key;
            _iv = _md5.ComputeHash(iv);
#if NET8_0_OR_GREATER
            //AES is Rijndael with a fixed 128-bit block: same algorithm, same ciphertext
            _crypto = Aes.Create();
#else
            _crypto = RijndaelManaged.Create();
#endif
            _crypto.Mode = CipherMode.CBC;
            _crypto.BlockSize = 128;
            _crypto.KeySize = 256;
            _crypto.Padding = PaddingMode.ISO10126;
        }

        public byte[] Encrypt(byte[] data)
        {
            lock (_encLock)
            {
                using (var encryptor = _crypto.CreateEncryptor(_key, _iv))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            lock (_decLock)
            {
                using (var decryptor = _crypto.CreateDecryptor(_key, _iv))
                {
                    return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }
        }

        public void Dispose()
        {
            _crypto.Dispose();
            _md5.Dispose();
        }
    }
}
