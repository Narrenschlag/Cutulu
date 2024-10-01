using Godot;

namespace Cutulu
{
    public static class Floatf
    {
        public static float Min(params float[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Mathf.Min(value, values[i]);
            }

            return value;
        }

        public static float Max(params float[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Mathf.Max(value, values[i]);
            }

            return value;
        }

        public static float IfNanDefault(this float value) => Mathf.IsNaN(value) ? default : value;

        // Converts a float (-1 to 1) to a byte (0 to 255)
        public static byte FloatToByte(this float value)
        {
            // Clamp the value to ensure it stays within the expected range
            value = Mathf.Clamp(value, -1f, 1f);

            // Scale from [-1, 1] to [0, 255]
            return (byte)((value + 1) * 127.5f);
        }

        // Converts a byte (0 to 255) back to a float (-1 to 1)
        public static float ByteToFloat(this byte value)
        {
            // Scale from [0, 255] to [-1, 1]
            return (value / 127.5f) - 1;
        }

        // Converts a float (0 to 1) to a byte (0 to 255)
        public static byte FloatToByte01(this float value)
        {
            // Clamp the value to ensure it stays within the expected range
            value = Mathf.Clamp(value, 0f, 1f);

            // Scale from [0, 1] to [0, 255]
            return (byte)Mathf.RoundToInt(value * 255f);
        }

        // Converts a byte (0 to 255) back to a float (-1 to 1)
        public static float ByteToFloat01(this byte value)
        {
            // Scale from [0, 255] to [0, 1]
            return value / 255f;
        }

        public static float AbsMod(this float value, float modulus)
        {
            var _value = value % modulus;

            return _value < 0 ? _value + modulus : _value;
        }
    }
}