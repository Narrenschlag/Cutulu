namespace Cutulu.Core
{
    public static class Random
    {
        /// <summary>
		///	Random value: [0 ]1
        /// </summary>
        public static float Value => System.Random.Shared.NextSingle();

        /// <summary>
		///	Random value: [min max]
        /// </summary>
        public static float Range(float minIncluded, float maxIncluded) => Value * (maxIncluded - minIncluded) + minIncluded;

        /// <summary>
		///	Random value: [min ]max
        /// </summary>
        public static int Range(int minIncluded, int maxExcluded) => minIncluded == maxExcluded - 1 ? minIncluded : Godot.Mathf.RoundToInt(Range(minIncluded, (float)(maxExcluded - 1)));

        /// <summary>
		///	Random value: [0 ]max
        /// </summary>
        public static int Range(int maxExcluded) => Range(0, maxExcluded);

        /// <summary>
		///	Random value: [min max]
        /// </summary>
        public static int RangeIncluded(int minIncluded, int maxIncluded) => Range(minIncluded, maxIncluded + 1);

        /// <summary>
		///	Random value: [0 max]
        /// </summary>
        public static int RangeIncluded(int maxIncluded) => Range(0, maxIncluded + 1);

        public static byte RandomByte() => (byte)RangeIncluded(0, byte.MaxValue);
        public static short RandomShort() => (short)RangeIncluded(short.MinValue, short.MaxValue);
        public static int RandomInt() => RangeIncluded(int.MinValue, int.MaxValue);
        public static long RandomLong() => (long)RandomInt() + RandomInt();
    }
}
