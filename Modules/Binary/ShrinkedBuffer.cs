using System;
using System.IO;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Used to shrink byte buffers to a minimum.
    /// Use the offset value for special edge cases.
    /// </summary>
    public struct ShrinkedBuffer
    {
        public Shrinked[] Elements;
        public byte[] Remaining;
        public byte[] Buffer;
        public byte Offset;
        public byte Depth;

        public ShrinkedBuffer(byte[] buffer, byte offset = 0)
        {
            Offset = offset;
            Depth = 1;

            // Open Streams
            var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);

            // Define Offset
            byte[] offsetBytes = reader.ReadBytes(offset);

            // Calculate Steps
            int steps = Mathf.FloorToInt((stream.Length) / 16f);

            // Shrink given 4x4 byte pairs at a time
            Elements = new Shrinked[steps];
            for (int i = 0; i < steps; i++)
            {
                Elements[i] = new(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            }

            // Write Remaining
            Remaining = reader.ReadBytes((byte)stream.Length);

            // Calculate Buffer
            stream = new();
            using var writer = new BinaryWriter(stream);

            writer.Write(offsetBytes);
            for (int i = 0; i < Elements.Length; i++)
            {
                writer.Write(Elements[i].GetBuffer());
            }

            writer.Write(Remaining);
            Buffer = stream.ToArray();

            // Close streams
            reader.Close();
            writer.Close();
        }

        public byte[] Decompress()
        {
            // Open Streams
            MemoryStream stream = new(Buffer);
            BinaryReader reader = new(stream);
            MemoryStream result = new();
            BinaryWriter writer = new(result);

            // Write Offset
            writer.Write(reader.ReadBytes(Offset));

            // Write shrinked
            int v;
            for (int i = 0; i < Elements.Length; i++)
            {
                for (byte k = 0; k < 4; k++)
                {
                    var value = Elements[i].GetValue(k, out _);

                    switch (value)
                    {
                        case sbyte @sbyte:
                            v = (sbyte)value;
                            break;

                        case short @short:
                            v = (short)value;
                            break;

                        case Int24 @int24:
                            v = ((Int24)value).ToInt32();
                            break;

                        default:
                            v = (int)value;
                            break;
                    }

                    writer.Write(v);
                }
            }

            // Write Remaining
            writer.Write(Remaining);

            // Store result buffer
            var buffer = result.ToArray();

            // Close Streams
            reader.Close();
            writer.Close();

            return buffer;
        }
    }
}