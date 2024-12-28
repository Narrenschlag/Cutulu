using System.IO;
using Godot;

namespace Cutulu.Core
{
    public static class Shrinkf
    {
        public static byte[] Compress(this byte[] buffer, out byte[] lengths)
        {
            // Calculate steps
            int steps = Mathf.FloorToInt(buffer.Length / 16f);
            lengths = new byte[steps];

            Debug.Log($"{buffer.Length} bytes => {steps} steps");

            // Define streams
            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);
            using var result = new MemoryStream();
            using var writer = new BinaryWriter(result);

            // Shrink given 4x4 byte pairs at a time
            var shrinked = new Shrinked[steps];
            for (int i = 0; i < steps; i++)
            {
                shrinked[i] = new Shrinked(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            }

            for (int i = 0; i < shrinked.Length; i++)
            {
                var _buffer = shrinked[i].GetBuffer();
                writer.Write(_buffer);

                Debug.Log($"16 bytes => {_buffer.Length} bytes");
            }

            // Write remaining bytes
            writer.Write(reader.ReadBytes((byte)stream.Length));
            buffer = result.ToArray();

            // Close streams
            reader.Close();
            writer.Close();

            return buffer;
        }

        public static byte[] Decompress(this byte[] buffer, byte[] lengths)
        {
            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);
            using var result = new MemoryStream();
            using var writer = new BinaryWriter(result);

            for (int i = 0; i < lengths.Length; i++)
            {
                var shrinked = new Shrinked(reader.ReadBytes(lengths[i]));

                for (byte k = 0; k < 4; k++)
                {
                    writer.Write(shrinked.GetBytesOf(k));
                }
            }

            writer.Write(reader.ReadBytes((int)stream.Length));
            buffer = result.ToArray();

            reader.Close();
            writer.Close();

            return result.ToArray();
        }
    }
}