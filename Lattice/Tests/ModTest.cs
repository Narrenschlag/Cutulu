namespace Cutulu.Lattice.Tests
{
    using System.Threading.Tasks;

    using Godot;
    using Core;

    public partial class ModTest : IntegrationTest
    {
        [Export] private int ExternalModCount { get; set; } = 0;
        [Export] private InternalMod[] Mods { get; set; }

        protected override int StepCount => 2;

        protected override async Task<bool> _Process()
        {
            var instances = ModLoader.Load(Mods, null, true);

            if (instances.Size() != ExternalModCount + Mods.Size())
            {
                PrintErr($"Expected {ExternalModCount + Mods.Size()} mods, but got {instances.Size()}");
                return false;
            }

            Print($"Loaded {instances.Length}/{ModLoader.Instances.Count} mods");
            NextStep();

            Print($"Activating mods... ({ModLoader.Instances.Count} mods)");
            ModLoader.Activate();

            if (AssetLoader.References.Count != 3)
            {
                PrintErr($"Expected 3 asset entries, but got {AssetLoader.References.Count}");
                return false;
            }

            Print($"Loaded {AssetLoader.References.Count} asset entries");
            NextStep();

            Print($"manifest-name: {(AssetLoader.TryGet("manifest", out string manifest) ? manifest : "<null>")}");
            Print($"icon-name: {(AssetLoader.TryGet("icon", out Texture2D icon) ? $"{icon.GetWidth()}x{icon.GetHeight()}px" : "<null>")}");
            Print($"box-name: {(AssetLoader.TryGet("box", out BoxShape3D box) ? box.Size : "<null>")} {(AssetLoader.TryGetSource("box", out var source) ? source.Name : "<null>")}");

            await Task.Delay(1);
            Application.Quit();
            return true;
        }
    }
}