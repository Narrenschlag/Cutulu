namespace Cutulu
{
	public static class Random
	{
		public static float Value => System.Random.Shared.NextSingle();

		public static float Range(float minIncluded, float maxIncluded) => Value * (maxIncluded - minIncluded) + minIncluded;

		public static int RangeInt(int minIncluded, int maxExcluded) => Godot.Mathf.RoundToInt(Range(minIncluded, maxExcluded - 1));
		public static int RangeInt(int maxExcluded) => RangeInt(0, maxExcluded);

		public static int RangeInt2(int minIncluded, int maxIncluded) => RangeInt(minIncluded, maxIncluded + 1);
		public static int RangeInt2(int maxIncluded) => RangeInt(0, maxIncluded + 1);
	}
}
