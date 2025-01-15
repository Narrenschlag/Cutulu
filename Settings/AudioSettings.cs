namespace Cutulu.Settings
{
    using System.Collections.Generic;
    using Core;

    public static class AudioSettings
    {
        public static T GetValue<T>(string key, T defaultValue) => AppData.GetAppData($"audio_settings/{key}", defaultValue);
        public static void SetValue<T>(string key, T value) => AppData.SetAppData($"audio_settings/{key}", value);

        public static System.Action Updated { get; set; }

        public static void LoadValues(params string[] channelNames)
        {
            MasterVolume = GetValue(nameof(MasterVolume), 1.0f);
            IsStereo = GetValue(nameof(IsStereo), true);

            foreach (var channel in channelNames)
            {
                var volume = GetChannelVolume(channel);
            }

            SetOutputDevice(GetOutputDevice());
        }

        public static readonly Dictionary<string, float> ChannelVolumes = new();

        private static float masterVolume = 1.0f;
        public static float MasterVolume
        {
            get => masterVolume;

            set
            {
                Godot.AudioServer.SetBusVolumeDb(0, (float)Godot.Mathf.LinearToDb(masterVolume = value));
                SetValue("Master", value);
            }
        }

        public static bool IsMono { get => !stereoEnabled; set => IsStereo = !value; }
        private static bool stereoEnabled = true;
        public static bool IsStereo
        {
            get => stereoEnabled; set
            {
                SetValue(nameof(IsStereo), value);
                stereoEnabled = value;
            }
        }

        public static void SetChannelVolume(string channel, float volume)
        {
            if (channel.IsEmpty()) return;

            var idx = Godot.AudioServer.GetBusIndex(channel);
            if (idx < 0) return;

            SetValue($"ch_{channel}", volume);
            ChannelVolumes[channel] = volume;

            Godot.AudioServer.SetBusVolumeDb(idx, (float)Godot.Mathf.LinearToDb(volume));
        }

        public static float GetChannelVolume(string channel, bool raw = false)
        {
            if (channel.IsEmpty()) return 1.0f;

            if (ChannelVolumes.TryGetValue(channel, out var volume) == false)
            {
                ChannelVolumes[channel] = volume = GetValue($"ch_{channel}", 1.0f);
            }

            return volume * (raw ? 1.0f : MasterVolume);
        }

        public static void SetOutputDevice(string device)
        {
            Godot.AudioServer.SetOutputDevice(device);
            SetValue("output_device", device);
        }

        public static string GetOutputDevice(string def = "Default")
        {
            return GetValue("output_device", def);
        }
    }
}