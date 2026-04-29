namespace Cutulu.Patching;

using System.Security.Cryptography;
using System;

internal static class ChunkHash
{
    public static string Compute(byte[] data)
        => Compute(data.AsSpan());

    public static string Compute(ReadOnlySpan<byte> data)
        => Convert.ToHexString(SHA256.HashData(data)).ToUpperInvariant();

    public static bool Verify(byte[] data, string expected)
        => string.Equals(Compute(data), expected, StringComparison.OrdinalIgnoreCase);
}