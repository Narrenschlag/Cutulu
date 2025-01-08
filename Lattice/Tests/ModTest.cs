namespace Cutulu.Lattice.Tests
{
    using System.Threading.Tasks;

    using Godot;
    using Core;

    public partial class ModTest : IntegrationTest
    {
        [Export] private int ExpectedAssetCount { get; set; } = 1;
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

            if (AssetLoader.References.Count != ExpectedAssetCount)
            {
                PrintErr($"Expected {ExpectedAssetCount} asset entries, but got {AssetLoader.References.Count}");
                return false;
            }

            Print($"Loaded {AssetLoader.References.Count} asset entries");
            NextStep();

            await Task.Delay(1);
            Application.Quit();
            return true;
        }
    }
}