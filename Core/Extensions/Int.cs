namespace Cutulu.Core
{
    using System;

    public static class Intf
    {
        public static int abs(this int i) => Math.Abs(i);

        public static int max(this int f0, int f1) => Math.Max(f0, f1);
        public static int max(this int f0, int f1, int f2) => max(max(f0, f1), f2);
        public static int max(this int f0, int f1, int f2, int f3) => max(max(f0, f1, f2), f3);

        public static int min(this int f0, int f1) => Math.Min(f0, f1);
        public static int min(this int f0, int f1, int f2) => min(min(f0, f1), f2);
        public static int min(this int f0, int f1, int f2, int f3) => min(min(f0, f1, f2), f3);

        public static float Min(params int[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Math.Min(value, values[i]);
            }

            return value;
        }


        public static int Max(params int[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Math.Max(value, values[i]);
            }

            return value;
        }

        public static int AbsMod(this int value, int modulus)
        {
            if (modulus == default) return value;
            var _value = value % modulus;

            return _value < 0 ? _value + modulus : _value;
        }
    }
}