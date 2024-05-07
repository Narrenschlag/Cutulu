using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class InputOverrideMap : Resource
    {
        [Export] public InputOverride[] Entries { get; set; }
    }
}