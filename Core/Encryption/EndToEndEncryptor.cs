namespace Cutulu.Encryption
{
    using System.Security.Cryptography;
    using System;

    public class EndToEndEncryptor
    {
        private readonly ECDiffieHellmanPublicKey _remotePublicKey;
        private readonly ECDiffieHellman _ecdh;

        /// <summary>
        /// Creates a new encryptor instance with optional keys.
        /// Provide privateKey for decrypting and publicKey for encrypting.
        /// </summary>
        public EndToEndEncryptor(byte[] privateKey = null, byte[] publicKey = null)
        {
            _ecdh = EndToEndEncryption.GetECDH(true);

            if (privateKey != null)
                _ecdh.ImportPkcs8PrivateKey(privateKey, out _);

            if (publicKey != null)
            {
                var temp = ECDiffieHellman.Create();
                temp.ImportSubjectPublicKeyInfo(publicKey, out _);
                _remotePublicKey = temp.PublicKey;
            }
        }

        /// <summary>
        /// Encrypts a message using the remote public key (must be provided in constructor).
        /// </summary>
        public byte[] Encrypt(byte[] plaintext)
        {
            if (_remotePublicKey == null)
                throw new InvalidOperationException("Public key not loaded. Cannot encrypt.");

            // Generate ephemeral key
            using var eph = EndToEndEncryption.GetECDH(true);
            var ephPub = eph.PublicKey.ExportSubjectPublicKeyInfo();

            // Derive shared secret with remote pub key
            var shared = eph.DeriveKeyFromHash(_remotePublicKey, HashAlgorithmName.SHA256);

            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
            var tag = new byte[EndToEndEncryption.TagSize];
            var ciphertext = new byte[plaintext.Length];

            using var aes = new AesGcm(shared, EndToEndEncryption.TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            return Combine(ephPub, nonce, tag, ciphertext);
        }

        /// <summary>
        /// Decrypts a message using the internal private key (must be provided in constructor).
        /// </summary>
        public byte[] Decrypt(byte[] encrypted)
        {
            if (_ecdh == null)
                throw new InvalidOperationException("Private key not loaded. Cannot decrypt.");

            int offset = 0;

            var ephPub = ReadSegment(encrypted, ref offset);
            var nonce = ReadSegment(encrypted, ref offset);
            var tag = ReadSegment(encrypted, ref offset);
            var ciphertext = encrypted.AsSpan(offset).ToArray();

            var temp = ECDiffieHellman.Create();
            temp.ImportSubjectPublicKeyInfo(ephPub, out _);
            var shared = _ecdh.DeriveKeyFromHash(temp.PublicKey, HashAlgorithmName.SHA256);

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(shared, EndToEndEncryption.TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }

        /// <summary>
        /// Generates a new key pair (private + public).
        /// </summary>
        public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeys() => EndToEndEncryption.GenerateKeys();

        // Utilities
        private static byte[] Combine(params byte[][] parts)
        {
            using var ms = new System.IO.MemoryStream();
            foreach (var part in parts)
            {
                ms.Write(BitConverter.GetBytes(part.Length));
                ms.Write(part);
            }
            return ms.ToArray();
        }

        private static byte[] ReadSegment(byte[] src, ref int offset)
        {
            int len = BitConverter.ToInt32(src, offset);
            offset += sizeof(int);
            var segment = new byte[len];
            Array.Copy(src, offset, segment, 0, len);
            offset += len;
            return segment;
        }
    }
}