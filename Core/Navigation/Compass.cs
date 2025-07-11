#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    using Godot;

    public partial class Compass : Node
    {
        /// <summary>
        /// Returns compass angle in degrees
        /// </summary>
        public static float NavigationAngle()
        => Godot.Input.GetGyroscope()[1].toDegrees();
    }
}
#endif