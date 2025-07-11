#if GODOT4_0_OR_GREATER
namespace Cutulu.Core
{
    /// <summary>
    /// Loads and writes indexed inputs to easily index whole sets of the device.
    /// </summary>
    public partial class InputPreset
    {
        public const string PRESET_PATH = $"{CONST.USER_PATH}Input_Presets/slot%.preset";

        public InputDevice.InputIndex[] Array { get; set; } = System.Array.Empty<InputDevice.InputIndex>();
        public InputDevice.Enum Type { get; set; } = InputDevice.Enum.Invalid;

        public InputPreset() { }

        public static InputPreset LoadFromFile(string slot)
        {
            return new File(GetPath(slot)).TryRead(out InputPreset preset) ? preset : null;
        }

        public void WriteToFile(string slot)
        {
            new File(GetPath(slot)).Write(this);
        }

        public static bool FileExists(string slot)
        {
            return GetPath(slot).PathExists();
        }

        private static string GetPath(string slot)
        {
            return PRESET_PATH.Replace("%", slot);
        }
    }
}
#endif