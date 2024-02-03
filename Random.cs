namespace Cutulu
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
        public static int RangeInt(int minIncluded, int maxExcluded) => Godot.Mathf.RoundToInt(Range(minIncluded, maxExcluded - 1));

        /// <summary>
		///	Random value: [0 ]max
        /// </summary>
        public static int RangeInt(int maxExcluded) => RangeInt(0, maxExcluded);

        /// <summary>
		///	Random value: [min max]
        /// </summary>
        public static int RangeInt2(int minIncluded, int maxIncluded) => RangeInt(minIncluded, maxIncluded + 1);

        /// <summary>
		///	Random value: [0 max]
        /// </summary>
        public static int RangeInt2(int maxIncluded) => RangeInt(0, maxIncluded + 1);
    }
}
