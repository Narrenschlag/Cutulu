using Godot;

namespace Cutulu
{
	public static class NoiseF
	{
		public static NoiseGenerator GenerateNoise(this int seed, Vector3 offset, float frequency = .01f, FastNoiseLite.NoiseTypeEnum type = FastNoiseLite.NoiseTypeEnum.Perlin)
		{
			FastNoiseLite fnl = new FastNoiseLite();

			fnl.Frequency = frequency;
			fnl.NoiseType = type;
			fnl.Offset = offset;
			fnl.Seed = seed;

			return new NoiseGenerator(fnl);
		}
	}

	public class NoiseGenerator
	{
		public FastNoiseLite noise;

		private Vector2 range;

		public NoiseGenerator(FastNoiseLite fnl)
		{
			this.range = new Vector2(0, 1);
			this.noise = fnl;

			noise.FractalLacunarity = 2f;
			noise.FractalGain = 0.5f;

			Type = FastNoiseLite.FractalTypeEnum.Fbm;
			Octaves = 1;
		}

		public float Lacunarity
		{
			set => noise.FractalLacunarity = value;
			get => noise.FractalLacunarity;
		}

		public float Gain
		{
			set => noise.FractalGain = value;
			get => noise.FractalGain;
		}

		public FastNoiseLite.FractalTypeEnum Type
		{
			set => noise.FractalType = value;
			get => noise.FractalType;
		}

		public int Octaves
		{
			set => noise.FractalOctaves = value;
			get => noise.FractalOctaves;
		}

		public int Seed
		{
			set => noise.Seed = value;
			get => noise.Seed;
		}

		public float Min
		{
			set => Range = Range.setX(value);
			get => Range.X;
		}

		public float Max
		{
			set => Range = Range.setY(value);
			get => Range.Y;
		}

		public Vector2 Range
		{
			set => range = new Vector2(Mathf.Min(value.X, value.Y), Mathf.Max(value.X, value.Y));
			get => range;
		}

		public float RangeGate(float value) => (value + 1) / 2 * (Max - Min) + Min;

		public float Value(float x) => RangeGate(noise.GetNoise1D(x));
		public float Value(float x, float y) => RangeGate(noise.GetNoise2D(x, y));
		public float Value(float x, float y, float z) => RangeGate(noise.GetNoise3D(x, y, z));
	}
}
