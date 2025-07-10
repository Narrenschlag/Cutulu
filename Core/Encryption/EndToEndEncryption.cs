namespace Cutulu.Encryption
{
    using System.Security.Cryptography;
    using System;

    /// <summary>
    /// Provides end-to-end encryption using ECDH (Elliptic Curve Diffie-Hellman) for key exchange
    /// and AES-GCM for symmetric encryption with authentication.
    /// </summary>
    public static class EndToEndEncryption
    {
        private static readonly ECCurve Curve = ECCurve.NamedCurves.nistP256;
        public const int TagSize = 16; // 128-bit authentication tag

        public static ECDiffieHellman GetECDH(bool curve) => curve ? ECDiffieHellman.Create(Curve) : ECDiffieHellman.Create();

        /// <summary>
        /// Generates a new ECDH key pair.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - PrivateKey: PKCS#8 formatted private key bytes
        /// - PublicKey: SubjectPublicKeyInfo (X.509) formatted public key bytes
        /// </returns>
        public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeys()
        {
            using var ecdh = GetECDH(true);

            var privateKey = ecdh.ExportPkcs8PrivateKey();
            var publicKey = ecdh.PublicKey.ExportSubjectPublicKeyInfo();

            return (privateKey, publicKey);
        }

        /// <summary>
        /// Encrypts a byte buffer using the recipient's public key.
        /// Uses an ephemeral key pair and AES-GCM with 256-bit shared secret.
        /// </summary>
        /// <param name="plaintext">The message to encrypt.</param>
        /// <param name="recipientPublicKey">The recipient's public key (X.509 format).</param>
        /// <returns>The encrypted message including ephemeral public key, nonce, tag, and ciphertext.</returns>
        public static byte[] Encrypt(byte[] plaintext, byte[] recipientPublicKey)
        {
            // Generate an ephemeral ECDH key pair
            using var ephemeral = GetECDH(true);
            var ephemeralPublicKey = ephemeral.PublicKey.ExportSubjectPublicKeyInfo();

            // Import recipient's public key
            var recipientKey = ImportPublic(recipientPublicKey);

            // Derive shared key using ECDH and SHA256
            var sharedKey = ephemeral.DeriveKeyFromHash(recipientKey, HashAlgorithmName.SHA256);

            // Generate random nonce (12 bytes for AES-GCM)
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);

            // Allocate tag and ciphertext buffers
            var tag = new byte[TagSize];
            var ciphertext = new byte[plaintext.Length];

            // Encrypt with AES-GCM
            using var aes = new AesGcm(sharedKey, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Combine all parts into a single buffer
            return Combine(ephemeralPublicKey, nonce, tag, ciphertext);
        }

        /// <summary>
        /// Decrypts a message previously encrypted with Encrypt().
        /// </summary>
        /// <param name="encrypted">The encrypted message (ephemeral pub key + nonce + tag + ciphertext).</param>
        /// <param name="privateKey">Your private key (PKCS#8 format).</param>
        /// <returns>The decrypted plaintext message.</returns>
        public static byte[] Decrypt(byte[] encrypted, byte[] privateKey)
        {
            int offset = 0;

            // Parse structure: [ephemeralPublicKey][nonce][tag][ciphertext]
            var ephemeralPublicKey = ReadSegment(encrypted, ref offset);
            var nonce = ReadSegment(encrypted, ref offset);
            var tag = ReadSegment(encrypted, ref offset);
            var ciphertext = encrypted.AsSpan(offset).ToArray();

            // Load private key
            using var ecdh = GetECDH(false);
            ecdh.ImportPkcs8PrivateKey(privateKey, out _);

            // Import sender's ephemeral public key
            var senderEphemeralKey = ImportPublic(ephemeralPublicKey);

            // Derive shared secret
            var sharedKey = ecdh.DeriveKeyFromHash(senderEphemeralKey, HashAlgorithmName.SHA256);

            // Decrypt
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(sharedKey, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }

        /// <summary>
        /// Imports a public key from a SubjectPublicKeyInfo (X.509) formatted byte array.
        /// </summary>
        private static ECDiffieHellmanPublicKey ImportPublic(byte[] pubBytes)
        {
            var temp = GetECDH(false);

            temp.ImportSubjectPublicKeyInfo(pubBytes, out _);

            return temp.PublicKey;
        }

        /// <summary>
        /// Combines multiple byte arrays into a single buffer with length-prefixing.
        /// </summary>
        private static byte[] Combine(params byte[][] parts)
        {
            using var ms = new System.IO.MemoryStream();

            foreach (var p in parts)
            {
                ms.Write(BitConverter.GetBytes(p.Length));
                ms.Write(p);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Reads a length-prefixed segment from a byte array.
        /// </summary>
        private static byte[] ReadSegment(byte[] src, ref int offset)
        {
            var len = BitConverter.ToInt32(src, offset);
            offset += sizeof(int);

            var segment = new byte[len];
            Array.Copy(src, offset, segment, 0, len);
            offset += len;

            return segment;
        }

        /// <summary>
        /// Returns public key based on private key.
        /// </summary>
        public static byte[] GetPublicKeyFromPrivateKey(byte[] privateKey)
        {
            using var ecdh = GetECDH(false);
            ecdh.ImportPkcs8PrivateKey(privateKey, out _);
            return ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        }
    }
}