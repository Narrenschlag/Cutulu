using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public class InputTranslation
    {
        protected Dictionary<InputCode, (JoyButton button, JoyAxis axis, string[] natives)> Overrides;

        public void Override(CustomTranslationMap overrideMap)
        {
            if (overrideMap.IsNull())
            {
                (Overrides ??= new()).Clear();

                for (int i = 0; i < overrideMap.Entries.Length; i++)
                {
                    if (Overrides.ContainsKey(overrideMap.Entries[i].Input) == false)
                    {
                        Overrides.Add(overrideMap.Entries[i].Input, (
                            overrideMap.Entries[i].GamepadButton,
                            overrideMap.Entries[i].GamepadAxis,
                            overrideMap.Entries[i].NativeGodot
                        ));
                    }
                }
            }
        }

        public virtual void Translate(InputCode input, out JoyButton button, out JoyAxis axis, out string[] natives)
        {
            // Override input
            if (Overrides != null && Overrides.TryGetValue(input, out var result))
            {
                natives = result.natives;
                button = result.button;
                axis = result.axis;
                return;
            }

            button = JoyButton.Invalid;
            axis = JoyAxis.Invalid;
            natives = null;

            switch (input)
            {
                case InputCode.Start:
                    button = JoyButton.Start;
                    break;

                case InputCode.Start2:
                    button = JoyButton.Misc1;
                    break;

                case InputCode.OSHome:
                    button = JoyButton.Guide;
                    break;



                case InputCode.LeftStickRight:
                    axis = JoyAxis.LeftX;
                    break;

                case InputCode.LeftStickUp:
                    axis = JoyAxis.LeftY;
                    break;

                case InputCode.RightStickRight:
                    axis = JoyAxis.RightX;
                    break;

                case InputCode.RightStickUp:
                    axis = JoyAxis.RightY;
                    break;



                case InputCode.LeftShoulder:
                    button = JoyButton.LeftShoulder;
                    break;

                case InputCode.LeftTrigger:
                    axis = JoyAxis.TriggerLeft;
                    break;

                case InputCode.RightShoulder:
                    button = JoyButton.RightShoulder;
                    break;

                case InputCode.RightTrigger:
                    axis = JoyAxis.TriggerRight;
                    break;



                case InputCode.LeftStickPress:
                    button = JoyButton.LeftStick;
                    break;

                case InputCode.RightStickPress:
                    button = JoyButton.RightStick;
                    break;



                case InputCode.LeftNorth:
                    button = JoyButton.DpadUp;
                    break;

                case InputCode.RightNorth:
                    button = JoyButton.Y;
                    break;

                case InputCode.LeftWest:
                    button = JoyButton.DpadLeft;
                    break;

                case InputCode.RightWest:
                    button = JoyButton.X;
                    break;

                case InputCode.LeftSouth:
                    button = JoyButton.DpadDown;
                    break;

                case InputCode.RightSouth:
                    button = JoyButton.A;
                    break;

                case InputCode.LeftEast:
                    button = JoyButton.DpadRight;
                    break;

                case InputCode.RightEast:
                    button = JoyButton.B;
                    break;

                default: break;
            }
        }
    }

    public enum InputCode : byte
    {
        Invalid,

        Start,
        Start2,
        OSHome,
        BothShoulders,

        LeftStickRight,
        LeftStickUp,

        RightStickRight,
        RightStickUp,

        RightShoulder,
        RightTrigger,

        LeftShoulder,
        LeftTrigger,

        RightStickPress,
        LeftStickPress,

        RightNorth,
        RightWest,
        RightSouth,
        RightEast,

        DpadRight,
        DpadUp,

        LeftNorth,
        LeftWest,
        LeftSouth,
        LeftEast
    }
}