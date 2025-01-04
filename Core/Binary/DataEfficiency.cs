using Godot;

namespace Cutulu.Core
{
    public static class DataEfficiency
    {
        #region Bind 8 bools to 1 Byte
        public static byte BindToByte(this bool bool0, params bool[] boolN)
        {
            byte @byte = BitBuilder.SetBit((byte)0, 0, bool0);

            if (boolN != null && boolN.Length > 0)
            {
                byte length = (byte)Mathf.Clamp(boolN.Length, 0, 7);

                for (byte n = 0; n < length; n++)
                {
                    @byte = BitBuilder.SetBit(@byte, n + 1, boolN[n]);
                }
            }

            return @byte;
        }

        public static bool[] UnBindToBools(this byte @byte)
        {
            bool[] result = new bool[8];

            for (byte n = 0; n < 8; n++)
            {
                result[n] = BitBuilder.GetBit(@byte, n);
            }

            return result;
        }
        #endregion
    }

    namespace Experimental
    {
        public class DateRange
        {
            public System.DateTime StartDate { get; set; }
            public System.DateTime EndDate { get; set; }

            public DateRange(System.DateTime startDate, System.DateTime endDate)
            {
                StartDate = startDate;
                EndDate = endDate;
            }
        }

        public class EmailAddress
        {
            public string Address { get; }
            public bool Valid { get; }

            public EmailAddress(string address)
            {
                Address = address;
                Valid = IsValid();
            }

            public bool IsValid() => Address.IsEmail();
        }

        public class GeoLocation
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public GeoLocation(double latitude, double longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }
        }

        public struct Money
        {
            public string Currency { get; }
            public float Amount { get; }

            public Money(float amount, string currency)
            {
                Amount = amount.Round(2);
                Currency = currency;
            }

            public static Money operator +(Money v1, Money v2) => v1.Merge(v2, 1f);

            /// <summary>
            /// Keep own currency, merge other's amount by calculating with course factor
            /// </summary>
            public readonly Money Merge(Money toMerge, float factorAtoB) => new(Amount + toMerge.Amount * factorAtoB, Currency);
        }

        public class int2
        {
            public int PackedValue { get; set; }

            public int Value1
            {
                get { return PackedValue & 0xFFFF; }
                set { PackedValue = (PackedValue & ~0xFFFF) | (value & 0xFFFF); }
            }

            public int Value2
            {
                get { return (PackedValue >> 16) & 0xFFFF; }
                set { PackedValue = (PackedValue & 0xFFFF) | ((value & 0xFFFF) << 16); }
            }

            public int2(int value1, int value2)
            {
                PackedValue = 0;

                Value1 = value1;
                Value2 = value2;
            }

            public static int2 operator +(int2 value1, int value2) => new(value1, value2);

            // Conversion from int2 to int
            public static implicit operator int(int2 value) => value.PackedValue;

            public int[] Values() => new int[2] { Value1, Value2 };
        }

        public class MusicalNote
        {
            public string Pitch { get; set; } // e.g., C, D, E, F, G, A, B
            public int Octave { get; set; }
            public double Duration { get; set; } // in seconds

            public MusicalNote(string pitch, int octave, double duration)
            {
                Pitch = pitch;
                Octave = octave;
                Duration = duration;
            }
        }
    }
}