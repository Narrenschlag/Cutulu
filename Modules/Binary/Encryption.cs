using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

namespace Cutulu
{
    public static class Encryption
    {
        /// <summary>Encrypts a string</summary>
        public static string EncryptString(this string plaintext, string encryption_key)
        {
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

        /// <summary>Decrypts a string</summary>
        public static string DecryptString(this string encrypted, string decryption_key)
        {
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