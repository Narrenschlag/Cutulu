using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System;

namespace Cutulu
{
    public class IdDictionary<T>
    {
        private Dictionary<int, T> Main;
        private Dictionary<T, int> Rvrs;
        private int lastIdx;

        public IdDictionary()
        {
            Main = new();
            Rvrs = new();
            lastIdx = 0;
        }

        // Indexer to set and get values using []
        public T this[int key]
        {
            get
            {
                if (Main.TryGetValue(key, out var value))
                    return value;

                throw new KeyNotFoundException($"Key {key} not found.");
            }

            set
            {
                Main[key] = value;
                Rvrs[value] = key;
            }
        }

        // Indexer to set and get values using []
        public int this[T key]
        {
            get
            {
                if (Rvrs.TryGetValue(key, out var idx))
                    return idx;

                throw new KeyNotFoundException($"Key {key} not found.");
            }
        }

        public T[] GetValues() => Main.Values.ToArray();

        public int Append(T value)
        {
            if (Rvrs.TryGetValue(value, out var idx))
            {
                return idx;
            }

            else
            {
                this[lastIdx] = value;
                return lastIdx++;
            }
        }

        public void Remove(int idx)
        {
            if (Main.TryGetValue(idx, out var val))
            {
                Main.Remove(idx);

                if (Rvrs.ContainsKey(val))
                    Rvrs.Remove(val);
            }
        }

        public void Remove(T val)
        {
            if (Rvrs.TryGetValue(val, out var idx))
            {
                Rvrs.Remove(val);

                if (Main.ContainsKey(idx))
                    Main.Remove(idx);
            }
        }

        public byte[] Encode()
        {
            if (Main.Count < 1) return Array.Empty<byte>();

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(lastIdx);
            writer.Write(Main.Count);

            foreach (var entry in Main)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value.Buffer());
            }

            return stream.ToArray();
        }

        public static IdDictionary<T> Decode(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            return Decode(reader);
        }

        public static IdDictionary<T> Decode(BinaryReader reader)
        {
            var result = new IdDictionary<T>();

            if (reader.BaseStream != null && reader.BaseStream.Length >= 8)
            {
                result.lastIdx = reader.ReadInt32();
                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var idx = reader.ReadInt32();

                    if (reader.TryBuffer<T>(out var val) == false) break;

                    result[idx] = val;
                }
            }

            return result;
        }

        public string Log()
        {
            var str = new StringBuilder();
            str.Append($"{Main.Count} entries.");

            foreach (var entry in Main)
            {
                str.Append($"\n{entry.Key}: {entry.Value:n2}");
            }

            return str.ToString();
        }
    }
}