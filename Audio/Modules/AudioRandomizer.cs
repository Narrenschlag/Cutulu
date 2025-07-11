#if GODOT4_0_OR_GREATER
namespace Cutulu.Audio
{
    using Godot;
    using Core;

    [GlobalClass]
    public partial class AudioRandomizer : AudioModule
    {
        [Export] public AudioModule[] Modules;

        public override AudioInstance GetInstance()
        {
            return Modules.RandomElement().GetInstance();
        }
    }
}
#endif