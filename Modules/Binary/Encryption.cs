using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System;

using Godot;

namespace Cutulu
{
    public static class Encryption
    {
        public static string HashEncryptString(this string plaintText, string key) => plaintText.EncryptString(key).EncryptString(key.HashPassword());
        public static string HashDecryptString(this string encryptedText, string key) => encryptedText.DecryptString(key.HashPassword()).DecryptString(key);

        public delegate void UpdateHandler(float progress);

        public static string EncryptString(this string plainText, string key, int depth)
        {
            // Encrypt
            for (int i = 0; i < depth; i++)
            {
                plainText = plainText.EncryptString(key = key.HashPassword());
            }

            return plainText;
        }

        public static string DecryptString(this string encryptedText, string key, int depth)
        {
            // Reverse hashing by using key
            var keys = new string[depth];
            for (int i = 0; i < depth; i++)
            {
                keys[depth - 1 - i] = key = key.HashPassword();
            }

            // Decrypt
            for (int i = 0; i < depth; i++)
            {
                encryptedText = encryptedText.DecryptString(keys[i]);
            }

            return encryptedText;
        }

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

        public static byte[] Encrypt(this byte[] data, string key)
        {
            if (data.IsEmpty() || string.IsNullOrEmpty(key))
                throw new ArgumentException("Data and key must not be null or empty.");

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Stretch the key to match the length of the plaintext
            byte[] stretchedKey = StretchKey(keyBytes, data.Length);

            // Perform XOR operation between plaintext and stretched key
            byte[] encryptedBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                encryptedBytes[i] = (byte)(data[i] ^ stretchedKey[i]);
            }

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? 1 : -1);
                Bytef.OffsetBits(ref encryptedBytes, ref offset);
            }

            return encryptedBytes;
        }

        public static async Task<byte[]> EncryptAsync(this byte[] data, string key, int performance, UpdateHandler onProgress = null)
        {
            if (data.IsEmpty() || string.IsNullOrEmpty(key))
                throw new ArgumentException("Data and key must not be null or empty.");

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Stretch the key to match the length of the plaintext
            byte[] stretchedKey = StretchKey(keyBytes, data.Length);

            // Perform XOR operation between plaintext and stretched key
            byte[] encryptedBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                encryptedBytes[i] = (byte)(data[i] ^ stretchedKey[i]);

                if (i % performance == 0)
                {
                    onProgress?.Invoke(i / (float)keyBytes.Length * .9f);
                    await Task.Delay(1);
                }
            }

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? 1 : -1);
                Bytef.OffsetBits(ref encryptedBytes, ref offset);

                if (i % performance == 0)
                {
                    onProgress?.Invoke(i / (float)keyBytes.Length * .1f + .9f);
                    await Task.Delay(1);
                }
            }

            return encryptedBytes;
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

        public static byte[] Decrypt(this byte[] data, string key)
        {
            if (data.IsEmpty() || string.IsNullOrEmpty(key))
                throw new ArgumentException("Encrypted text and key must not be null or empty.");

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? -1 : 1);
                Bytef.OffsetBits(ref data, ref offset);
            }

            // Stretch the key to match the length of the encrypted data
            byte[] stretchedKey = StretchKey(keyBytes, data.Length);

            // Perform XOR operation between encrypted data and stretched key to decrypt
            byte[] decryptedBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                decryptedBytes[i] = (byte)(data[i] ^ stretchedKey[i]);
            }

            return decryptedBytes;
        }

        public static async Task<byte[]> DecryptAsync(this byte[] data, string key, int performance, UpdateHandler onProgress = null)
        {
            if (data.IsEmpty() || string.IsNullOrEmpty(key))
                throw new ArgumentException("Encrypted text and key must not be null or empty.");

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // Offset bits based on second bytes in key
            for (int i = 0, offset; i < keyBytes.Length; i++)
            {
                offset = keyBytes[i] * (i % 2 == 0 ? -1 : 1);
                Bytef.OffsetBits(ref data, ref offset);

                if (i % performance == 0)
                {
                    onProgress?.Invoke(i / (float)keyBytes.Length * .1f);
                    await Task.Delay(1);
                }
            }

            // Stretch the key to match the length of the encrypted data
            byte[] stretchedKey = StretchKey(keyBytes, data.Length);

            // Perform XOR operation between encrypted data and stretched key to decrypt
            byte[] decryptedBytes = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                decryptedBytes[i] = (byte)(data[i] ^ stretchedKey[i]);

                if (i % performance == 0)
                {
                    onProgress?.Invoke(i / (float)data.Length * .9f + .1f);
                    await Task.Delay(1);
                }
            }

            return decryptedBytes;
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

        public static byte[] Hash(this byte[] input)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(input);
        }

        public static float Hash(this Vector2 value) => Hash(value.X, value.Y);
        public static float Hash(float hash1, float hash2)
        {
            // Choose prime numbers for best distribution
            const int prime1 = 17;
            const int prime2 = 31;

            // Simple combination using multiplication and addition
            return prime1 * hash1 + prime2 * hash2;
        }

        public static int Hash(int hash1, int hash2)
        {
            // Choose prime numbers for best distribution
            const int prime1 = 17;
            const int prime2 = 31;

            // Simple combination using multiplication and addition
            return prime1 * hash1 + prime2 * hash2;
        }
    }
}