namespace Cutulu.Lattice.Tests
{
    using System.Threading.Tasks;

    using Godot;
    using Core;

    public partial class PathTest : IntegrationTest
    {
        [Export] private InternalMod Mod { get; set; }

        protected override int StepCount => 1;

        protected override async Task<bool> _Process()
        {
            var manifest = Mod.ReadAssetEntries();

            for (var i = 0; i < manifest.Length; i++)
            {
                CoreBridge.Log($"{manifest[i].Name}: {manifest[i].Path}");
            }

            Application.Quit();
            return true;
        }
    }
}