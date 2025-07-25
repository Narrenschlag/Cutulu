namespace Cutulu.Core
{
    using System;

    public static class Floatf
    {
        public static readonly float Pi = (float)Math.PI;

        public static float abs(this float f) => Math.Abs(f);

        public static float max(this float f0, float f1) => Math.Max(f0, f1);
        public static float max(this float f0, float f1, float f2) => max(max(f0, f1), f2);
        public static float max(this float f0, float f1, float f2, float f3) => max(max(f0, f1, f2), f3);

        public static float min(this float f0, float f1) => Math.Min(f0, f1);
        public static float min(this float f0, float f1, float f2) => min(min(f0, f1), f2);
        public static float min(this float f0, float f1, float f2, float f3) => min(min(f0, f1, f2), f3);

        public static float toDegrees(this float radians) => radians / Pi * 180;
        public static float toRadians(this float degree) => degree / 180 * Pi;

        public static float GetAngleToFront180(this float fromAngle, float toAngle, bool useRadians = false)
        {
            // Convert angles to radians if needed
            if (useRadians == false)
            {
                fromAngle = fromAngle.toRadians();
                toAngle = toAngle.toRadians();
            }

            // Calculate the difference between the angles
            float delta = toAngle - fromAngle;

            // Wrap the delta within the range of -Pi to Pi (or -180 to 180 degrees)
            delta = (delta + Pi) % (Pi * 2);

            // Ensure the result is in the range of 0 to 180 degrees and inverted
            delta = Math.Abs(delta);
            if (delta > Pi)
                delta = 2 * Pi - delta;
            delta = Pi - delta;

            // Convert delta back to degrees if needed
            return useRadians ? delta : delta.toDegrees();
        }

        public static float Min(params float[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Math.Min(value, values[i]);
            }

            return value;
        }

        public static float Max(params float[] values)
        {
            var value = values[0];

            for (byte i = 1; i < values.Length && i < byte.MaxValue; i++)
            {
                value = Math.Max(value, values[i]);
            }

            return value;
        }

        public static float IfNanDefault(this float value)
        {
            return float.IsNaN(value) ? default : value;
        }

        // Converts a float (-1 to 1) to a byte (0 to 255)
        public static byte FloatToByte(this float value)
        {
            // Clamp the value to ensure it stays within the expected range
            value = Math.Clamp(value, -1f, 1f);

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
            value = Math.Clamp(value, 0f, 1f);

            // Scale from [0, 1] to [0, 255]
            return (byte)Math.Round(value * 255f);
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