using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

namespace Cutulu
{
    public static class Encryption
    {
        public static string EncryptString(this string plaintext, string key)
        {
            if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(key))
                throw new ArgumentException("Plaintext and key must not be null or empty.");

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Stretch the key to match the length of the plaintext
            byte[] stretchedKey = StretchKey(keyBytes, plaintextBytes.Length);

            // Perform XOR operation between plaintext and stretched key
            byte[] encryptedBytes = new byte[plaintextBytes.Length];
            for (int i = 0; i < plaintextBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plaintextBytes[i] ^ stretchedKey[i]);
            }

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? 1 : -1);
                Bytef.OffsetBits(ref encryptedBytes, ref offset);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        public static string DecryptString(this string encrypted, string key)
        {
            if (string.IsNullOrEmpty(encrypted) || string.IsNullOrEmpty(key))
                throw new ArgumentException("Encrypted text and key must not be null or empty.");

            byte[] encryptedBytes = Convert.FromBase64String(encrypted);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? -1 : 1);
                Bytef.OffsetBits(ref encryptedBytes, ref offset);
            }

            // Stretch the key to match the length of the encrypted data
            byte[] stretchedKey = StretchKey(keyBytes, encryptedBytes.Length);

            // Perform XOR operation between encrypted data and stretched key to decrypt
            byte[] decryptedBytes = new byte[encryptedBytes.Length];
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ stretchedKey[i]);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private static byte[] StretchKey(byte[] keyBytes, int length)
        {
            byte[] stretchedKey = new byte[length];
            for (int i = 0; i < length; i++)
            {
                stretchedKey[i] = keyBytes[i % keyBytes.Length];
            }
            return stretchedKey;
        }

        /// <summary>
        /// Encrypts a string
        /// </summary>
        public static string EncryptStringAes(this string plaintext, string encryption_key)
        {
            if (plaintext.IsEmpty()) return "";

            // Convert the plaintext string to a byte array
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Derive a new password using the PBKDF2 algorithm and a random salt
            Rfc2898DeriveBytes passwordBytes = new(encryption_key, 20);

            // Use the password to encrypt the plaintext
            Aes encryptor = Aes.Create();

            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plaintextBytes, 0, plaintextBytes.Length);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Decrypts a string
        /// </summary>
        public static string DecryptStringAes(this string encrypted, string decryption_key)
        {
            if (encrypted.IsEmpty()) return "";

            // Convert the encrypted string to a byte array
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);

            // Derive the password using the PBKDF2 algorithm
            Rfc2898DeriveBytes passwordBytes = new(decryption_key, 20);

            // Use the password to decrypt the encrypted string
            Aes encryptor = Aes.Create();

            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static string HashPassword(this string password)
        {
            using SHA256 sha256 = SHA256.Create();

            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Convert the byte array to a hexadecimal string
            StringBuilder builder = new();
            foreach (byte b in hashedBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}