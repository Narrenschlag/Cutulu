namespace Cutulu.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Godot;

    public static class InterRune
    {
        private static readonly Dictionary<char, byte> startNum = new();
        public static Dictionary<char, byte> StartNum
        {
            get
            {
                if (startNum.Count < 1)
                {
                    for (byte i = 0; i < Start.Length; i++)
                    {
                        startNum[Start[i]] = i;
                    }
                }

                return startNum;
            }
        }

        private static readonly Dictionary<char, byte> centerNum = new();
        public static Dictionary<char, byte> CenterNum
        {
            get
            {
                if (centerNum.Count < 1)
                {
                    for (byte i = 0; i < Center.Length; i++)
                    {
                        centerNum[Center[i]] = i;
                    }
                }

                return centerNum;
            }
        }

        private static readonly Dictionary<char, byte> endNum = new();
        public static Dictionary<char, byte> EndNum
        {
            get
            {
                if (endNum.Count < 1)
                {
                    for (byte i = 0; i < End.Length; i++)
                    {
                        endNum[End[i]] = i;
                    }
                }

                return endNum;
            }
        }

        public static readonly char[] Start = new[]{
            'm', 'b', 'd', 't', 'n', 'w', 's', 'k',
        };

        public static readonly char[] Center = new[]{
            'a', 'e', 'i', 'o'
        };

        public static readonly char[] End = new[]{
            'm', 'b', 'f', 't', 'k', 'n', 's', 'l',
        };

        public const char MultiChar = 'u';

        public static byte[] AsBytes(this string rune)
        {
            if (rune.IsEmpty() || (rune = rune.Trim()).Length < 3) throw new ArgumentException("A rune has to consist of at least 3 characters");

            var memory = new MemoryStream();
            var lastByte = default(byte);

            for (int i = 0; i < rune.Length; i++)
            {
                if (Center.Contains(rune[i]))
                {
                    var x = i > 0 ? -1 : +1;
                    var z = i >= rune.Length - 1 ? -1 : +1;

                    memory.WriteByte(lastByte = AsByte(rune[i + x], rune[i], rune[i + z]));
                }

                else if (rune[i] == MultiChar)
                {
                    var length = EndNum[rune[i + 1]] + 1;

                    for (byte k = 0; k < length; k++)
                    {
                        memory.WriteByte(lastByte);
                    }
                }
            }

            var array = memory.ToArray();
            memory.Close();
            return array;
        }

        public static string AsRune(this byte[] values)
        {
            if (values.IsEmpty()) return string.Empty;

            var stringBuilder = new StringBuilder();
            var lastChar = ' ';

            for (int i = 0; i < values.Length; i++)
            {
                var rune = AsRune(values[i]);

                // Handle write
                {
                    if (i > 0 ? lastChar != rune[0] : rune[0] != rune[2])
                    {
                        stringBuilder.Append(rune[0]);
                    }

                    stringBuilder.Append(rune[1]);

                    if (i < values.Length - 1 || rune[0] != rune[2])
                    {
                        stringBuilder.Append(lastChar = rune[2]);
                    }
                }

                // Handle multiple
                {
                    byte count = 0;
                    for (byte k = 1; k < End.Length && i + k < values.Length; k++)
                    {
                        if (values[i + k] == values[i]) count++;
                        else break;
                    }

                    if (count > 0)
                    {
                        stringBuilder.Append(MultiChar);
                        stringBuilder.Append(lastChar = End[count - 1]);

                        i += count;
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public static string AsRune(this byte value)
        {
            var f = (float)value / Center.Length;
            var i = Mathf.FloorToInt(f);
            var r = Mathf.RoundToInt(f % 1f * Center.Length);

            var a = Start[i % Start.Length];
            var b = Center[r];
            var c = End[Mathf.FloorToInt(i / Start.Length)];

            return $"{a}{b}{c}";
        }

        public static byte AsByte(char a, char b, char c)
        {
            var x = StartNum[a];
            var y = (float)CenterNum[b] / Center.Length;
            var z = EndNum[c] * Start.Length;

            return (byte)((x + y + z) * Center.Length);
        }
    }
}