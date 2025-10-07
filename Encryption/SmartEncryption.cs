namespace Cutulu.Encryption;

using System.Security.Cryptography;
using System;

public static class SmartEncryption
{
    private const int NonceSize = 12;  // Recommended for AES-GCM
    private const int TagSize = 16;    // Authentication tag size in bytes
    public const int KeySize = 32;    // 256-bit key

    // Generates a random 256-bit key
    public static byte[] GenerateKey()
    {
        var key = new byte[KeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    // Encrypts a plaintext buffer
    public static byte[] Encrypt(this byte[] buffer, byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.");

        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[buffer.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, buffer, ciphertext, tag);

        // Combine nonce + tag + ciphertext
        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return result;
    }

    // Decrypts a ciphertext buffer
    public static byte[] Decrypt(this byte[] buffer, byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.");

        if (buffer.Length < NonceSize + TagSize)
            throw new ArgumentException("Encrypted data is too short.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[buffer.Length - NonceSize - TagSize];

        Buffer.BlockCopy(buffer, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(buffer, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(buffer, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public static byte[] GetRandomBytes(int count)
    {
        var buffer = new byte[count];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);
        return buffer;
    }
}
