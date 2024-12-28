namespace Cutulu.Core.Audio
{
    using Godot;

    [GlobalClass]
    public partial class DModule : Resource
    {
        public virtual DModInstance GetInstance()
        {
            return default;
        }
    }
}