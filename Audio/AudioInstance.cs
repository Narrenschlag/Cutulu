namespace Cutulu.Audio
{
    using Core;

    public partial struct AudioInstance
    {
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public Godot.AudioStream Stream { get; set; }

        public AudioInstance() : this(default) { }
        public AudioInstance(Godot.AudioStream stream) : this(stream, 0f, 1f) { }
        public AudioInstance(Godot.AudioStream stream, float volume = 0f, float pitch = 1f)
        {
            Stream = stream;

            Volume = volume;
            Pitch = pitch;
        }
    }
}