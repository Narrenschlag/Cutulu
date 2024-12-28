namespace Cutulu.Core.Audio
{
    using Godot;

    [GlobalClass]
    public partial class DModRandomizer : DModule
    {
        [Export] public DModule[] Modules;

        public override DModInstance GetInstance()
        {
            return Modules.RandomElement().GetInstance();
        }
    }
}