using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class CustomTranslationMap : Resource
    {
        [Export] public CustomTranslationEntry[] Entries { get; set; }
    }
}