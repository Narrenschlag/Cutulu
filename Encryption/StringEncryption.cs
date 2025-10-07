namespace Cutulu.Encryption
{
    using System.Security.Cryptography;
    using Cutulu.Core;
    using System.Text;
    using System.IO;
    using System;

    public static class StringEncryption
    {
        private const int KeySize = 32;    // 256-bit
        private const int NonceSize = 12;  // Recommended for AES-GCM
        private const int TagSize = 16;    // 128-bit authentication tag
        private const int SaltSize = 16;   // Salt for PBKDF2

        public static string EncryptStringGcm(this string plaintext, string password)
        {
            if (plaintext.IsEmpty()) throw new ArgumentNullException(nameof(plaintext));
            if (password.IsEmpty()) throw new ArgumentNullException(nameof(password));

            var salt = GetRandomBytes(SaltSize);
            var nonce = GetRandomBytes(NonceSize);
            var key = DeriveKeyFromPassword(password, salt);
            var tag = new byte[TagSize];

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            using var ms = new MemoryStream();
            ms.Write(salt);
            ms.Write(nonce);
            ms.Write(tag);
            ms.Write(ciphertext);
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptStringGcm(this string base64Input, string password)
        {
            var input = Convert.FromBase64String(base64Input);

            int offset = 0;
            var salt = input.AsSpan(offset, SaltSize).ToArray(); offset += SaltSize;
            var nonce = input.AsSpan(offset, NonceSize).ToArray(); offset += NonceSize;
            var tag = input.AsSpan(offset, TagSize).ToArray(); offset += TagSize;
            var ciphertext = input.AsSpan(offset).ToArray();

            var key = DeriveKeyFromPassword(password, salt);
            var plaintextBytes = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            const int Iterations = 100_000;
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }

        public static byte[] GetRandomBytes(int count) => SmartEncryption.GetRandomBytes(count);
    }
}