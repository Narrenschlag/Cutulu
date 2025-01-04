using System;

namespace Cutulu.Core
{
    public struct intCantor
    {
        public uint PackedValue;

        public intCantor(uint x, uint y)
        {
            PackedValue = Pack(x, y);
        }

        public (uint X, uint Y) XY()
        {
            Unpack(PackedValue, out uint X, out uint Y);
            return (X, Y);
        }

        public static uint Pack(uint x, uint y) => ((x + y) * (x + y + 1) / 2) + y;

        // Function to unpack a single integer into the original pair
        public static void Unpack(uint packed, out uint x, out uint y)
        {
            uint t = (uint)Math.Floor((-1 + Math.Sqrt(1 + 8 * packed)) / 2);
            uint w = (t * t + t) / 2;
            y = packed - w;
            x = t - y;
        }
    }
}