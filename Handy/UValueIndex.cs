using System.Collections.Generic;
using System.Linq;

namespace Cutulu
{
    public class UValueIndex<Value>
    {
        protected readonly Dictionary<Value, int> Recursive;
        protected readonly Dictionary<int, Value> Index;
        protected int NextIdx;

        public UValueIndex()
        {
            NextIdx = int.MinValue;
            Recursive = new();
            Index = new();
        }

        public int Add(Value point)
        {
            if (Recursive.TryGetValue(point, out var idx)) return idx;

            Recursive.Add(point, NextIdx);
            Index.Add(NextIdx, point);

            _ChangeValue();
            return NextIdx++;
        }

        public void Remove(int idx)
        {
            if (Index.TryGetValue(idx, out var point))
            {
                Recursive.Remove(point);
                Index.Remove(idx);

                _ChangeValue();
            }
        }

        public void Remove(Value point)
        {
            if (Recursive.TryGetValue(point, out var idx))
            {
                Recursive.Remove(point);
                Index.Remove(idx);

                _ChangeValue();
            }
        }

        public void Clear()
        {
            Recursive.Clear();
            Index.Clear();
        }

        public Value[] Values => Index.Values.ToArray();
        public int[] Keys => Index.Keys.ToArray();
        public int Length => Index.Count;

        protected virtual void _ChangeValue()
        {

        }
    }
}