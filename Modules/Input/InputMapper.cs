using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public class InputMapper
    {
        protected readonly Dictionary<InputCode, (JoyButton button, JoyAxis axis, string native)> Overrides;

        public InputMapper(InputOverrideMap @override) : this(@override.Entries) { }
        public InputMapper(params InputOverride[] overrides)
        {
            Overrides ??= new();
            Override(overrides);
        }

        public InputMapper(InputMapper @base, InputOverrideMap @override) : this(@base, @override.Entries) { }
        public InputMapper(InputMapper @base, params InputOverride[] overrides)



        {
            Overrides = new(@base.Overrides);
            Override(overrides);
        }

        public void Override(params InputOverride[] overrides)
        {
            for (int i = 0; i < overrides?.Length; i++)
            {
                if (overrides[i] != null)
                {
                    if (Overrides.ContainsKey(overrides[i].Input) == false)
                    {
                        Overrides.Add(overrides[i].Input, (
                            overrides[i].GamepadButton,
                            overrides[i].GamepadAxis,
                            overrides[i].NativeGodot
                        ));
                    }

                    else
                    {
                        Overrides[overrides[i].Input] = (
                            overrides[i].GamepadButton,
                            overrides[i].GamepadAxis,
                            overrides[i].NativeGodot
                        );
                    }
                }
            }
        }

        public void Clear() => Overrides.Clear();
        public void Clear(params InputCode[] overrides)
        {
            for (int i = 0; i < overrides?.Length; i++)
            {
                if (Overrides.ContainsKey(overrides[i]))
                {
                    Overrides.Remove(overrides[i]);
                }
            }
        }

        public virtual void Translate(InputCode input, out JoyButton button, out JoyAxis axis, out string native)
        {
            // Override input
            if (Overrides != null && Overrides.TryGetValue(input, out var result))
            {
                native = result.native;
                button = result.button;
                axis = result.axis;
                return;
            }

            button = JoyButton.Invalid;
            axis = JoyAxis.Invalid;
            native = null;

            switch (input)
            {
                case InputCode.OSHome:
                    button = JoyButton.Guide;
                    break;



                case InputCode.RStart:
                    button = JoyButton.Start;
                    break;

                case InputCode.LStart:
                    button = JoyButton.Misc1;
                    break;



                case InputCode.RStickEast:
                    axis = JoyAxis.RightX;
                    break;

                case InputCode.LStickEast:
                    axis = JoyAxis.LeftX;
                    break;



                case InputCode.RStickNorth:
                    axis = JoyAxis.RightY;
                    break;

                case InputCode.LStickNorth:
                    axis = JoyAxis.LeftY;
                    break;



                case InputCode.RStickPress:
                    button = JoyButton.RightStick;
                    break;

                case InputCode.LStickPress:
                    button = JoyButton.LeftStick;
                    break;



                case InputCode.RShoulder:
                    button = JoyButton.RightShoulder;
                    break;

                case InputCode.LShoulder:
                    button = JoyButton.LeftShoulder;
                    break;



                case InputCode.RTrigger:
                    axis = JoyAxis.TriggerRight;
                    break;

                case InputCode.LTrigger:
                    axis = JoyAxis.TriggerLeft;
                    break;



                case InputCode.RNorth:
                    button = JoyButton.Y;
                    break;

                case InputCode.LNorth:
                    button = JoyButton.DpadUp;
                    break;



                case InputCode.RWest:
                    button = JoyButton.X;
                    break;

                case InputCode.LWest:
                    button = JoyButton.DpadLeft;
                    break;



                case InputCode.RSouth:
                    button = JoyButton.A;
                    break;

                case InputCode.LSouth:
                    button = JoyButton.DpadDown;
                    break;



                case InputCode.REast:
                    button = JoyButton.B;
                    break;

                case InputCode.LEast:
                    button = JoyButton.DpadRight;
                    break;

                default: break;
            }
        }
    }

    public enum InputCode : byte
    {
        Invalid,

        #region Buttons
        OSHome,

        RStart,
        LStart,

        RShoulder,
        LShoulder,

        RTrigger,
        LTrigger,

        RStickPress,
        LStickPress,
        #endregion

        #region Axies
        RStickNorth = 16,
        LStickNorth = 32,
        RNorth = 64,
        LNorth = 128,

        RStickWest = 17,
        LStickWest = 33,
        RWest = 65,
        LWest = 129,

        RStickSouth = 18,
        LStickSouth = 34,
        RSouth = 66,
        LSouth = 130,

        RStickEast = 19,
        LStickEast = 35,
        REast = 67,
        LEast = 131,
        #endregion
    }
}