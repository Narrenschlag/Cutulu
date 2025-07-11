#if GODOT4_0_OR_GREATER
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
#endif