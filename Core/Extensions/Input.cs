namespace Cutulu.Core
{
#if GODOT4_0_OR_GREATER
    using Godot;
#endif

    public static class Inputf
    {
#if GODOT4_0_OR_GREATER
        public static bool GetKey(this string name, float threshhold = .5f) => GetValue(name) >= threshhold;
        public static float GetValue(this string name) => Godot.Input.GetActionRawStrength(name);

        public static Vector2 MousePosition(this Node node, bool clampToScreen = false)
        {
            var screen = node.GetViewport().GetMousePosition();

            if (clampToScreen)
                screen = screen.Clamp(Vector2.Zero, DisplayServer.WindowGetSize());

            return screen;
        }

        public static Vector2I MousePosition() => DisplayServer.MouseGetPosition();

        public static bool GetMousePosition(this Camera3D camera, out Vector3 globalPosition, uint mask = 4294967295)
        {
            if (RaycastFromCamera(camera, out RaycastHit hit, mask))
            {
                globalPosition = hit.Point;
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

            Godot.Input.MouseMode = capturedMouse ? Godot.Input.MouseModeEnum.Captured : Godot.Input.MouseModeEnum.Visible;
        }
#endif
    }
}