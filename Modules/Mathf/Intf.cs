using Godot;

namespace Cutulu
{
    public static class Intf
    {
        public static float Min(params int[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Mathf.Min(value, values[i]);
            }

            return value;
        }


        public static int Max(params int[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Mathf.Max(value, values[i]);
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