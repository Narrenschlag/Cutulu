using Godot;

namespace Cutulu.Core
{
    public readonly partial struct HexagonRuleCase
    {
        public static readonly char[] OnChars = ['1', 'i', 'I'];

        public readonly bool[] Neighbours;
        public readonly byte Length;

        public HexagonRuleCase(Vector2I axial, IHexagonRuleCourt court)
        {
            Neighbours = new bool[Hexagon2.Neighbours.Length];
            Length = 0;

            for (byte i = 0; i < Neighbours.Length; i++)
            {
                if (Neighbours[i] = court.CanConnectRule(axial + Hexagon2.Neighbours[i]))
                    Length++;
            }
        }

        public HexagonRuleCase(string rules)
        {
            Neighbours = new bool[Hexagon2.Neighbours.Length];
            Length = 0;

            if (rules.IsEmpty()) return;

            rules = rules.Trim();
            var i = 0;

            foreach (var c in rules)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (Neighbours[i++] = OnChars.Contains(c))
                        Length++;
                }
            }
        }

        public new readonly string ToString()
        {
            var str = "";

            for (var i = 0; i < Neighbours.Length; i++)
            {
                str += Neighbours[i] ? "1" : "0";
            }

            return str;
        }

        public readonly void Reverse()
        {
            System.Array.Reverse(Neighbours);
        }

        public readonly void Offset(int offset)
        {
            if (offset == 0) return;

            var arr = new bool[Neighbours.Length];

            System.Array.Copy(Neighbours, arr, Neighbours.Length);

            for (var i = 0; i < Neighbours.Length; i++)
            {
                Neighbours[i] = arr.ModulatedElement(i + offset);
            }
        }

        public readonly void Flip()
        {
            var length = Mathf.FloorToInt(Neighbours.Length / 2f);

            for (var i = 0; i < length; i++)
            {
                (Neighbours[i], Neighbours[^(i + 1)]) = (Neighbours[^(i + 1)], Neighbours[i]);
            }
        }

        public readonly void Invert()
        {
            for (var i = 0; i < Neighbours.Length; i++)
            {
                Neighbours[i] = !Neighbours[i];
            }
        }

        public readonly bool Compare(HexagonRuleCase[] cases, out int idx, out byte rotation)
        {
            if (cases.NotEmpty())
            {
                for (idx = 0; idx < cases.Length; idx++)
                {
                    if (Compare(cases[idx], out rotation))
                        return true;
                }
            }

            rotation = 0;
            idx = 0;

            return false;
        }

        public readonly bool Compare(HexagonRuleCase other, out byte rotation)
        {
            rotation = 0;

            if (Length != other.Length) return false;

            for (byte i = 0; i < Neighbours.Length; i++, rotation++)
            {
                var rotated = Neighbours.MoveElements(i);

                if (rotated.SequenceEquals(other.Neighbours))
                    return true;
            }

            return false;
        }
    }

    public interface IHexagonRuleCourt
    {
        public bool CanConnectRule(Vector2I axial);
    }
}