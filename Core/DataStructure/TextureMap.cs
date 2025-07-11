#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using System.Collections.Generic;
    using Godot;

    public struct TextureMap
    {
        public readonly Dictionary<int, List<Vector2I>> Dictionary = [];
        private int[,] Map { get; set; }

        /// <summary>
        /// Returns -1 for pixels that are not contained in mapper
        /// </summary>
        public TextureMap(Texture2D texture, Dictionary<Color, int> mapper) : this(texture, mapper, (Vector2I)texture.GetSize(), Vector2I.Zero) { }
        public TextureMap(Texture2D texture, Dictionary<Color, int> mapper, Vector2I Size, Vector2I Offset)
        {
            Image image = texture.GetImage();
            Map = new int[Size.X, Size.Y];

            for (int y = Offset.Y; y < Size.Y + Offset.Y; y++)
            {
                for (int x = Offset.X; x < Size.X + Offset.X; x++)
                {
                    int value = mapper.TryGetValue(image.GetPixel(x, y), out int v) ? v : -1;
                    Map[x - Offset.X, y - Offset.Y] = value;

                    if (value >= 0)
                    {
                        if (!Dictionary.TryGetValue(value, out List<Vector2I> list))
                        {
                            Dictionary.Add(value, list = []);
                        }

                        list.Add(new Vector2I(x, y) - Offset);
                    }
                }
            }
        }

        public readonly int GetHeight() => Map.GetLength(1);
        public readonly int GetWidth() => Map.GetLength(0);
        public readonly int this[int x, int y]
        {
            get => Map[x, y];
        }
    }
}
#endif