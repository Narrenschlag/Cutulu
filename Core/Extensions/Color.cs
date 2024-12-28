namespace Cutulu.Core
{
    using Godot;

    public static class ColorExtension
    {
        private readonly static Vector3[] LerpMatrix = new Vector3[] {
            new (1, 0, 0),
            new (1, 1, 0),
            new (0, 1, 0),
            new (0, 1, 1),
            new (0, 0, 1),
            new (1, 0, 1),
            new (1, 0, 0),
        };

        public static Color Evaluate(float value, float alpha = 1f)
        {
            value = value.AbsMod(1f);

            var max = LerpMatrix.Length - 1;
            var end = Mathf.CeilToInt(value * max);
            var start = Mathf.FloorToInt(value * max);
            var vec = LerpMatrix[start].Lerp(LerpMatrix[end], (value * max) - start);

            return new(vec.X, vec.Y, vec.Z, alpha);
        }
    }
}