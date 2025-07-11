namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Collections;
    using System;

    public struct BoolStore : IEnumerable<bool>
    {
        public byte[] FormattableBoolBuffer { readonly get => BoolBuffer; set => BoolBuffer = value; }

        private readonly int Capacity;
        private byte[] BoolBuffer;

        public BoolStore(int capacity, params bool[] values)
        {
            BoolBuffer = new byte[(int)Math.Floor(capacity / 8f)];
            Capacity = capacity;

            for (int i = 0; i < capacity && i < values.Length; i++)
            {
                Bytef.SetBit(ref BoolBuffer, (ushort)i, values[i]);
            }
        }

        // Indexer
        public bool this[int index]
        {
            set => Bytef.SetBit(ref BoolBuffer, (ushort)index, value);
            readonly get => Bytef.GetBit(BoolBuffer, index);
        }

        // IEnumerable implementation
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // IEnumerable<T> implementation
        public readonly IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < Capacity; i++)
            {
                yield return this[i];
            }
        }
    }
}