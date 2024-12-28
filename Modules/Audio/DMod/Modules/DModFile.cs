namespace Cutulu.Core.Audio
{
    using Godot;

    [GlobalClass]
    public partial class DModFile : DModule
    {
        [Export] public AudioStream File;
        [Export] public Vector2 Volume = Vector2.Zero;
        [Export] public Vector2 Pitch = Vector2.One;

        public override DModInstance GetInstance()
        {
            return new()
            {
                Volume = GetVolume(),
                Pitch = GetPitch(),
                Stream = GetStream(),
            };
        }

        public virtual float GetVolume() => Random.Range(Mathf.Min(Volume.X, Volume.Y), Mathf.Max(Volume.X, Volume.Y));
        public virtual float GetPitch() => Mathf.Max(Random.Range(Mathf.Min(Pitch.X, Pitch.Y), Mathf.Max(Pitch.X, Pitch.Y)), 0.01f);
        public virtual AudioStream GetStream() => File;
    }
}