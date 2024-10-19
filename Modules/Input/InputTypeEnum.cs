namespace Cutulu.Input
{
    public enum InputTypeEnum : byte
    {
        Invalid = 255,

        Axis = 0,
        AxisButton = 1,
        Button = 2,

        Mouse = 10,
        Key = 11,
    }
}