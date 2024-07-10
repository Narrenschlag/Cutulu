using Godot;

namespace Cutulu
{
    public static class Inputf
    {
        public static bool GetKey(this string name, float threshhold = .5f) => GetValue(name) >= threshhold;
        public static float GetValue(this string name) => Input.GetActionRawStrength(name);

        public static Vector2 MousePosition(this Node node, bool clampToScreen = false)
        {
            var screen = node.GetViewport().GetMousePosition();

            if (clampToScreen)
                screen = screen.Clamp(Vector2.Zero, DisplayServer.WindowGetSize());

            return screen;
        }

        public static bool GetMousePosition(this Camera3D camera, out Vector3 globalPosition, uint mask = 4294967295)
        {
            if (RaycastFromCamera(camera, out RaycastHit hit, mask))
            {
                globalPosition = hit.point;
                return true;
            }

            globalPosition = Vector3.Zero;
            return false;
        }

        public static void GetRayAt(this Camera3D camera, Vector2 screenPosition, out Vector3 origin, out Vector3 direction)
        {
            direction = camera.ProjectRayNormal(screenPosition);
            origin = camera.ProjectRayOrigin(screenPosition);
        }

        public static Vector3 GetHit(this Camera3D camera, float y = 0) => GetHit(camera, MousePosition(camera), y);
        public static Vector3 GetHit(this Camera3D camera, Vector2 screenPosition, float y = 0)
        {
            GetRayAt(camera, screenPosition, out var origin, out var direction);
            return Trianglef.RayToY(origin, direction, y);
        }

        public static bool RaycastFromCamera(this Camera3D camera, out RaycastHit hit, uint mask = 4294967295)
        => Physics.Raycast(camera, out hit, camera.Far, mask);

        public static bool Down(this string name, ref bool valueStore, float threshold = .5f)
        {
            bool old = valueStore;

            valueStore = name.GetKey(threshold);
            return !old && valueStore;
        }

        public static bool Up(this string name, ref bool valueStore, float threshold = .5f)
        {
            bool old = valueStore;

            valueStore = name.GetKey(threshold);
            return old && !valueStore;
        }

        private static bool capturedMouse;
        public static void CaptureMouse() => CaptureMouse(!capturedMouse);
        public static void CaptureMouse(bool value)
        {
            capturedMouse = value;

            Input.MouseMode = capturedMouse ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }

        #region Server - Client Communication
        public static byte ReadBaseInputs()
        {
            byte inputs = 0, i;

            // Movement
            Core.SetBitAt(ref inputs, i = 0, "move_up".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_right".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_down".GetKey());
            Core.SetBitAt(ref inputs, ++i, "move_left".GetKey());

            // Jump, Sneak
            Core.SetBitAt(ref inputs, ++i, "jump".GetKey());
            Core.SetBitAt(ref inputs, ++i, "sneak".GetKey());

            // Actions
            Core.SetBitAt(ref inputs, ++i, "attack".GetKey());
            Core.SetBitAt(ref inputs, ++i, "interact".GetKey());

            return inputs;
        }

        public static (Vector2 movement, bool jump, bool sneak, bool attack, bool interact) ReadBaseInputs(this byte inputByte)
        {
            return
                new(
                    // Movement
                    new Vector2(
                        inputByte.GetBitAt_float(3) - inputByte.GetBitAt_float(1), // Inverted to fix wrong movement on x axis
                        inputByte.GetBitAt_float(0) - inputByte.GetBitAt_float(2)
                        ),

                    // Jump, Sneak
                    inputByte.GetBitAt(4),
                    inputByte.GetBitAt(5),

                    // Actions
                    inputByte.GetBitAt(6),
                    inputByte.GetBitAt(7)
                );
        }
        #endregion
    }
}
