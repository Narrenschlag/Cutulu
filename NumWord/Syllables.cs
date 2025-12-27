using Godot;

namespace Cutulu.NumWord
{
    public static class Syllables
    {
        public const int LetterLength = 25;

        public const int IndexOfA = 97;
        public const int IndexOfZ = 122;

        public const int ByteBy3 = 85;

        public static readonly char[] Start = new[]{ // 20
            'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z',
        };

        public static readonly char[] End = new[]{ // 3
            'm', 'n', 't',
        };

        public static readonly char[] Vokale = new[]{ // 5
            'a', 'e', 'i', 'o', 'u', // ignore y
        };

        public static string GetSyllable(this byte value)
        {
            var f = (value + 1f) / 5f; // 0 - 51,2
            var i = Mathf.FloorToInt(f);

            var b = Vokale[(int)(f % 1f * 5)];
            var a = Start[i % 20];
            var c = End[Mathf.FloorToInt(i)];

            return $"{a}{b}{c}";
        }
    }
}