namespace Cutulu.Audio
{
    public partial struct DModInstance
    {
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public Godot.AudioStream Stream { get; set; }
    }
}