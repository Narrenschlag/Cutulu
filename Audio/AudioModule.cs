namespace Cutulu.Audio
{
    using Godot;
    using Core;

    [GlobalClass]
    public partial class AudioModule : Resource
    {
        public virtual AudioInstance GetInstance()
        {
            return default;
        }
    }
}