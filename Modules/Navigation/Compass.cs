using Godot;

namespace Cutulu
{
    public partial class Compass : Node
    {
        /// <summary>
        /// Returns compass angle in degrees
        /// </summary>
        public static float NavigationAngle()
        => Input.GetGyroscope()[1].toDegrees();
    }
}