using Godot;

namespace Cutulu
{
    public static class Convertf
    {
        #region From Float
        public static uint ToUInt(this float value, byte decimals = 2)
        => (uint)Mathf.FloorToInt(value * Mathf.Pow(10, decimals));

        public static int ToInt(this float value, byte decimals = 2)
        => Mathf.FloorToInt(value * Mathf.Pow(10, decimals));

        public static ushort ToUShort(this float value, byte decimals = 2)
        => (ushort)Mathf.Clamp(Mathf.FloorToInt(value * Mathf.Pow(10, decimals)), 0, ushort.MaxValue);

        public static short ToShort(this float value, byte decimals = 2)
        => (short)Mathf.Clamp(Mathf.FloorToInt(value * Mathf.Pow(10, decimals)), short.MinValue, short.MaxValue);

        public static byte ToByte(this float value, byte decimals = 2)
        => (byte)Mathf.Clamp(Mathf.FloorToInt(value * Mathf.Pow(10, decimals)), 0, byte.MaxValue);

        public static sbyte ToSByte(this float value, byte decimals = 2)
        => (sbyte)Mathf.Clamp(Mathf.FloorToInt(value * Mathf.Pow(10, decimals)), sbyte.MinValue, sbyte.MaxValue);
        #endregion

        #region To Float
        public static float ToFloat(this uint value, byte decimals = 2)
        => (float)value / Mathf.Pow(10, decimals);

        public static float ToFloat(this int value, byte decimals = 2)
        => (float)value / Mathf.Pow(10, decimals);

        public static float ToFloat(this ushort value, byte decimals = 2)
        => (float)value / Mathf.Pow(10, decimals);

        public static float ToFloat(this short value, byte decimals = 2)
        => (float)value / Mathf.Pow(10, decimals);

        public static float ToFloat(this byte value, byte decimals = 2)
        => value / Mathf.Pow(10, decimals);

        public static float ToFloat(this sbyte value, byte decimals = 2)
        => value / Mathf.Pow(10, decimals);
        #endregion
    }
}