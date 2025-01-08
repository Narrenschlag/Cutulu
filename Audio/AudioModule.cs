namespace Cutulu.Audio
{
    using Godot;

    [GlobalClass]
    public partial class AudioModule : Resource
    {
        public virtual AudioInstance GetInstance()
        {
            return default;
        }
    }
}