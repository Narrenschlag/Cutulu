namespace Cutulu.Core
{
    /// <summary>
    /// Contains information about a certain input
    /// </summary>
    public partial struct InputKey
    {
        public Enum Type { get; set; }
        public int Id { get; set; }

        public InputKey(Enum type, int id)
        {
            Type = type;
            Id = id;
        }

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