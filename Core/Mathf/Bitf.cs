namespace Cutulu.Core; ///META_COPY///

public static partial class Bitf ///META_COPY///
{ ///META_COPY///
    public static byte GetByte(params bool[] bits)
    {
        sbyte length = (sbyte)(Math.Min(bits.Length, 8) - 1);
        byte result = 0;

        for (sbyte i = length; i >= 0; i--)
        {
            if (bits[length - i]) result |= (byte)(1 << i);
        }

        return result;
    }

    ///META_COPY_START///
    public static Span<bool> GetBits(this byte value)
    {
        byte length = 8;

        Span<bool> span = new(new bool[length--]);

        for (byte i = 0; i <= length; i++)
        {
            span[length - i] = (value & (byte)(1 << i)) != 0;
        }

        return span;
    }

    /// <summary>
    /// Sets bit to 0: false
    /// </summary>
    public static byte DisableBit(this byte b, byte i) => (byte)(b & (1 << i));

    /// <summary>
    /// Sets bit to 1: true
    /// </summary>
    public static byte EnableBit(this byte b, byte i) => (byte)(b | (1 << i));

    /// <summary>
    /// Gets bit.
    /// </summary>
    public static bool GetBit(byte b, byte i) => (b & (byte)(1 << i)) != 0;

    public static byte SetBit(this byte b, byte i, bool value)
    {
        return (byte)(value ? b | 1 << i : b & ~(1 << i));
    }
    ///META_COPY_END///
} ///META_COPY///