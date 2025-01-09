namespace Cutulu.Lattice.Tests
{
    using System.Threading.Tasks;

    using Godot;
    using Core;

    public partial class PathTest : IntegrationTest
    {
        [Export] private InternalMod Mod { get; set; }
        [Export] private string[] Paths { get; set; }

        protected override int StepCount => 1;

        protected override async Task<bool> _Process()
        {
            var manifest = Mod.ReadAssetEntries();

            for (var i = 0; i < manifest.Length; i++)
            {
                CoreBridge.Log($"{manifest[i].Name}: {manifest[i].Path}");
            }

            foreach (var path in Paths)
            {
                var relative = Parser.MakePathRelative(path, Mod.ResourcePath);
                CoreBridge.Log($"{path} -> {relative}");
            }

            await Task.Delay(1);
            Application.Quit();
            return true;
        }
    }
}