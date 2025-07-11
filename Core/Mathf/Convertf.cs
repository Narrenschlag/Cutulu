namespace Cutulu.Core
{
    using System;

    public static class Convertf
    {
        #region From Float
        public static uint ToUInt(this float value, byte decimals = 2)
        => (uint)Math.Floor(value * Math.Pow(10, decimals));

        public static int ToInt(this float value, byte decimals = 2)
        => (int)Math.Floor(value * Math.Pow(10, decimals));

        public static ushort ToUShort(this float value, byte decimals = 2)
        => (ushort)Math.Clamp(Math.Floor(value * Math.Pow(10, decimals)), 0, ushort.MaxValue);

        public static short ToShort(this float value, byte decimals = 2)
        => (short)Math.Clamp(Math.Floor(value * Math.Pow(10, decimals)), short.MinValue, short.MaxValue);

        public static byte ToByte(this float value, byte decimals = 2)
        => (byte)Math.Clamp(Math.Floor(value * Math.Pow(10, decimals)), 0, byte.MaxValue);

        public static sbyte ToSByte(this float value, byte decimals = 2)
        => (sbyte)Math.Clamp(Math.Floor(value * Math.Pow(10, decimals)), sbyte.MinValue, sbyte.MaxValue);
        #endregion

        #region To Float
        public static float ToFloat(this uint value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);

        public static float ToFloat(this int value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);

        public static float ToFloat(this ushort value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);

        public static float ToFloat(this short value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);

        public static float ToFloat(this byte value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);

        public static float ToFloat(this sbyte value, byte decimals = 2)
        => value / (float)Math.Pow(10, decimals);
        #endregion
    }
}