using System;
using System.Security.Cryptography;
using System.Text;

namespace VedionScreenShare.Services
{
    /// <summary>
    /// AES-256 encryption/decryption for frame data
    /// </summary>
    public class EncryptionService
    {
        private byte[] _key;

        /// <summary>
        /// Initialize with Base64-encoded 256-bit key
        /// </summary>
        public EncryptionService(string base64Key)
        {
            _key = Convert.FromBase64String(base64Key);
            if (_key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes (256 bits)", nameof(base64Key));
        }

        /// <summary>
        /// Generate a new random 256-bit key
        /// </summary>
        public static string GenerateKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Encrypt data and return (Base64 ciphertext, Base64 IV)
        /// </summary>
        public (string ciphertext, string iv) Encrypt(byte[] plaintext)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                byte[] iv = aes.IV;
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                    return (
                        Convert.ToBase64String(ciphertext),
                        Convert.ToBase64String(iv)
                    );
                }
            }
        }

        /// <summary>
        /// Decrypt Base64 ciphertext with given Base64 IV
        /// </summary>
        public byte[] Decrypt(string base64Ciphertext, string base64Iv)
        {
            byte[] ciphertext = Convert.FromBase64String(base64Ciphertext);
            byte[] iv = Convert.FromBase64String(base64Iv);

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
        }
    }
}
