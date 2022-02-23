using System;
using System.IO;
using System.Security.Cryptography;

namespace Blockchain
{
    interface IHashable
    {
        MemoryStream GetHashable();
    }
    static class Crypto
    {
        private static SHA256 sha256 = SHA256.Create();
        private static RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public static byte[] CalculateSHA256(IHashable hashable)
        {
            MemoryStream ms = hashable.GetHashable();
            byte[] hash = sha256.ComputeHash(ms.ToArray());
            ms.Dispose();
            return hash;
        }
        public static byte[] CalculateSHA256(byte[] buffer)
        {
            return sha256.ComputeHash(buffer);
        }
        public static byte[] GetRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);
            return randomBytes;
        }
        public static long GetTimestamp()
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
        public static CngKey GenerateECDSAKey()
        {
                return new ECDsaCng().Key;
        }
        public static byte[] SignHash(byte[] hash, CngKey key)
        {
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                return ecdsa.SignHash(hash);
            }
        }
        public static bool VerifyHash(byte[] hash, byte[] signature, byte[] PK)
        {
            using (CngKey key = CngKey.Import(PK, CngKeyBlobFormat.EccPublicBlob))
            {
                using (ECDsaCng ecdsa = new ECDsaCng(key))
                {
                    return ecdsa.VerifyHash(hash, signature);
                }
            }
        }
    }
}