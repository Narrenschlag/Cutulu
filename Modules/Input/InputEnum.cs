namespace Cutulu.Input
{
    using Godot;

    public enum InputEnum : int
    {
        KeyOffset = -1024,
        MouseOffset = -512,
        ButtonOffset = -256,
        AxisButtonOffset = -128,
        Invalid = -1,

        Axis0 = (int)JoyAxis.LeftX,
        Axis1,
        Axis2,
        Axis3,
        Axis4,
        Axis5,

        AxisButton0 = -AxisButtonOffset,
        AxisButton1,
        AxisButton2,
        AxisButton3,
        AxisButton4,
        AxisButton5,
        AxisButton6,
        AxisButton7,
        AxisButton8,
        AxisButton9,
        AxisButton10,
        AxisButton11,

        Button0 = (int)JoyButton.A - ButtonOffset,
        Button1,
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        Button13,
        Button14,
        Button15,
        Button16,
        Button17,
        Button18,
        Button19,
        Button20,
        Button21,

        MouseMin = (int)MouseButton.Left - MouseOffset,
        MouseMax = MouseMin + (int)MouseButton.Xbutton2 - (int)MouseButton.Left,

        KeyMin = (int)Key.Space - KeyOffset,
        KeyMax = KeyMin + (int)Key.Kp9 - (int)Key.Space,
    }
}