namespace Cutulu.Core;

public ref struct LocalRandom(int seed)
{
    private readonly System.Random Source = new(seed);

    public LocalRandom() : this(Random.RandomInt()) { }

    /// <summary>
    ///	Random value: [0 ]1
    /// </summary>
    public float Value => Source.NextSingle();

    /// <summary>
    ///	Random value: [min max]
    /// </summary>
    public float Range(float minIncluded, float maxIncluded) => Value * (maxIncluded - minIncluded) + minIncluded;

    /// <summary>
    ///	Random value: [min ]max
    /// </summary>
    public int Range(int minIncluded, int maxExcluded) => minIncluded == maxExcluded - 1 ? minIncluded : (int)System.Math.Round(Range(minIncluded, (float)(maxExcluded - 1)));

    /// <summary>
    ///	Random value: [0 ]max
    /// </summary>
    public int Range(int maxExcluded) => Range(0, maxExcluded);

    /// <summary>
    ///	Random value: [min max]
    /// </summary>
    public int RangeIncluded(int minIncluded, int maxIncluded) => Range(minIncluded, maxIncluded + 1);

    /// <summary>
    ///	Random value: [0 max]
    /// </summary>
    public int RangeIncluded(int maxIncluded) => Range(0, maxIncluded + 1);

    public byte RandomByte() => (byte)RangeIncluded(0, byte.MaxValue);
    public short RandomShort() => (short)RangeIncluded(short.MinValue, short.MaxValue);
    public int RandomInt() => RangeIncluded(int.MinValue, int.MaxValue);
    public long RandomLong() => (long)RandomInt() + RandomInt();
}