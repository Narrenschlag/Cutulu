namespace Cutulu.Core;

using Godot;

public static class Controlf
{
    public static bool IsPointOver(this Vector2 screenPosition, Control control)
    {
        // Get the control's rectangle in global (screen) coordinates
        return control.GetGlobalRect().HasPoint(screenPosition);
    }

    public static bool IsMouseOver(this Control control) => IsPointOver(control.MousePosition(), control);
}