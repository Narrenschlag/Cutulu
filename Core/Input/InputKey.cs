namespace Cutulu.Core
{
    /// <summary>
    /// Contains information about a certain input
    /// </summary>
    public partial struct InputKey(InputKey.Enum type, int id)
    {
        public Enum Type { get; set; } = type;
        public int Id { get; set; } = id;

        public enum Enum : byte
        {
            Invalid = 255,

            Keyboard = 0,
            MouseButton,
            MouseAxis,
            JoyButton,
            JoyAxis,
            Midi,
        }
    }
}