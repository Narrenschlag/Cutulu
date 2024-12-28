namespace Cutulu.Core
{
    /// <summary>
    /// Loads and writes indexed inputs to easily index whole sets of the device.
    /// </summary>
    public partial class InputPreset
    {
        public const string PRESET_PATH = $"{IO.USER_PATH}Input_Presets/slot%.preset";

        public InputDevice.InputIndex[] Array { get; set; } = System.Array.Empty<InputDevice.InputIndex>();
        public InputDevice.Enum Type { get; set; } = InputDevice.Enum.Invalid;

        public InputPreset() { }

        public static InputPreset LoadFromFile(string slot)
        {
            return IO.TryRead(GetPath(slot), out InputPreset preset, IO.FileType.Binary) ? preset : null;
        }

        public void WriteToFile(string slot)
        {
            IO.Write(this, GetPath(slot), IO.FileType.Binary);
        }

        public static bool FileExists(string slot)
        {
            return IO.Exists(GetPath(slot));
        }

        private static string GetPath(string slot)
        {
            return PRESET_PATH.Replace("%", slot);
        }
    }
}