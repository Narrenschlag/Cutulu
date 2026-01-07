namespace Cutulu.Core;

public static partial class Bitf
{
    public static byte GetByte(params bool[] bits)
    {
        sbyte length = (sbyte)(System.Math.Min(bits.Length, 8) - 1);
        byte result = 0;

        for (sbyte i = length; i >= 0; i--)
        {
            if (bits[length - i]) result |= (byte)(1 << i);
        }

        return result;
    }

    public static System.Span<bool> GetBits(this byte value)
    {
        byte length = 8;

        System.Span<bool> span = new(new bool[length--]);

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
    public static bool GetBit(this byte b, byte i) => (b & (byte)(1 << i)) != 0;

    public static byte SetBit(this byte b, byte i, bool value)
    {
        return (byte)(value ? b | 1 << i : b & ~(1 << i));
    }
}