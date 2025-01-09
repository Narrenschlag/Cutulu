namespace Cutulu.Lattice
{
    public class ModInstance
    {
        public readonly IMod Source;

        public bool Enabled { get; set; } // Set true by interface
        public int LoadOrder { get; set; } // Set priority by interface

        public bool Active { get; private set; } // Set active when activated

        public AssemblyInstance Assembly => AssemblyLoader.Instances.TryGetValue(Source, out var instance) ? instance : null;
        public AssetInstance Asset => AssetLoader.Instances.TryGetValue(Source, out var instance) ? instance : null;

        public ModInstance(IMod source)
        {
            Enabled = false;
            LoadOrder = 0;

            (Source = source)?.Load();
        }

        public void Unload() => Source?.Unload();

        public void Activate()
        {
            Active = true;

            Source?.Activate();
        }

        public void Deactivate()
        {
            Active = false;

            Source?.Deactivate();
        }
    }
}