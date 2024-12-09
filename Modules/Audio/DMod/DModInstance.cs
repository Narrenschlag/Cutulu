namespace Cutulu.Audio
{
    public partial struct DModInstance
    {
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public Godot.AudioStream Stream { get; set; }

        public DModInstance() : this(default) { }
        public DModInstance(Godot.AudioStream stream) : this(stream, 0f, 1f) { }
        public DModInstance(Godot.AudioStream stream, float volume = 0f, float pitch = 1f)
        {
            Stream = stream;

            Volume = volume;
            Pitch = pitch;
        }
    }
}