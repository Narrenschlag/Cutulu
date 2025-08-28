namespace Cutulu.Core;

using System.IO;

public partial struct ActionTimeStamp
{
    public ushort MilliSecond { get; set; }
    public byte Second { get; set; }
    public ushort Minute { get; set; }

    public ActionTimeStamp() { }

    public static ActionTimeStamp GetTimeStamp() //int offset = 0)
    {
        var time = Godot.Time.GetTicksMsec(); //+ (uint)int.Max(0, offset);

        var mil = time % 1000;
        time -= mil;

        var sec = time % 60;
        time -= sec;

        return new()
        {
            MilliSecond = (ushort)mil,
            Second = (byte)sec,
            Minute = (ushort)time,
        };
    }

    class Encoder : BinaryEncoder<ActionTimeStamp>
    {
        public override void Encode(BinaryWriter writer, ref object value)
        {
            if (value is not ActionTimeStamp t) return;

            writer.Write(t.MilliSecond);
            writer.Write(t.Second);
            writer.Write(t.Minute);
        }

        public override object Decode(BinaryReader reader)
        {
            return new ActionTimeStamp()
            {
                MilliSecond = reader.ReadUInt16(),
                Second = reader.ReadByte(),
                Minute = reader.ReadUInt16(),
            };
        }
    }
}