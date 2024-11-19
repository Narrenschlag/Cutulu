namespace Cutulu
{
    using System;
    using Godot;

    public static class Core
    {
        #region Shortcuts               ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Node GetRoot(this Node node) => node.GetTree().CurrentScene;

        public static Node3D Main3D { get { if (_main3d == null) _main3d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node3D>(); return _main3d; } }
        private static Node3D _main3d;

        public static Node2D Main2D { get { if (_main2d == null) _main2d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node2D>(); return _main2d; } }
        private static Node2D _main2d;

        public static Node Main { get { if (_main == null) _main = (Engine.GetMainLoop() as SceneTree).CurrentScene; return _main; } }
        private static Node _main;
        #endregion

        #region General Functions       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float invert(this float value, bool invert) => value * (invert ? -1 : 1);
        public static int invert(this int value, bool invert) => value * (invert ? -1 : 1);

        public static int toInt(this bool value, int _true, int _false) => value ? _true : _false;
        public static int toInt01(this bool value) => value ? 1 : 0;
        #endregion

        #region Bit Functions           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float GetBitAt_float(this byte number, int bitIndex) => GetBitAt((int)number, bitIndex) ? 1 : 0;
        public static bool GetBitAt(this byte number, int bitIndex) => GetBitAt((int)number, bitIndex);
        public static bool GetBitAt(this int number, int bitIndex)
        {
            if (bitIndex < 0 || bitIndex >= 32) // C# Integer have 32 bits
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");

            return ((number >> bitIndex) & 1) == 1;
        }

        public static byte SetBitAt(ref byte number, int bitIndex, bool newValue) => number = SetBitAt(number, bitIndex, newValue);
        public static byte SetBitAt(this byte number, int bitIndex, bool newValue) => (byte)SetBitAt((int)number, bitIndex, newValue);

        public static int SetBitAt(ref int number, int bitIndex, bool newValue) => number = SetBitAt(number, bitIndex, newValue);
        public static int SetBitAt(this int number, int bitIndex, bool newValue)
        {
            if (bitIndex < 0 || bitIndex >= 32) // C# Integer have 32 bits
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");

            // Set bit to 0
            number &= ~(1 << bitIndex);

            // If newValue is true, set bit to 1
            if (newValue) number |= (1 << bitIndex);

            return number;
        }

        public static bool[] GetBitsArray(this int number)
        {
            int numBits = 32; // Annahme: Ein int hat 32 Bits in C#
            bool[] bits = new bool[numBits];

            for (int i = 0; i < numBits; i++)
            {
                bits[i] = ((number >> i) & 1) == 1;
            }

            System.Array.Reverse(bits); // Umkehrung, um die richtige Reihenfolge zu erhalten
            return bits;
        }
        #endregion

        #region Input Functions         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float GetInputValue(this string name) => Godot.Input.GetActionRawStrength(name);

        public static Vector2 GetInputVector(this string negativeX, string positiveX, string negativeY, string positiveY) => new(GetInputAxis(negativeX, positiveX), GetInputAxis(negativeY, positiveY));
        public static float GetInputAxis(this string negative, string positive) => -negative.GetInputValue() + positive.GetInputValue();
        public static bool GetInput(this string name, float threshold = .2f) => GetInputValue(name) >= threshold;
        #endregion
    }
}
