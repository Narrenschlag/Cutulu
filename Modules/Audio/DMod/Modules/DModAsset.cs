namespace Cutulu.Audio
{
    using Godot;

    [GlobalClass]
    public partial class DModAsset : DModule
    {
        [Export] public string StreamAsset;
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
        public virtual AudioStream GetStream() => DMod.AssetLibrary is AssetLibrary instance && instance.TryGet(StreamAsset, out AudioStream stream) ? stream : null;
    }
}