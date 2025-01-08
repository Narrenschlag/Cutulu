namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    using Core;

    public class AssemblyInstance
    {
        public readonly List<string> Entries = new();

        private readonly IMod Source;

        public AssemblyInstance(IMod source)
        {
            Source = source;

            Register();
        }

        public void Register()
        {
            var entries = Source.ReadAssemblyEntries();

            if (entries.IsEmpty()) return;

            foreach (var (_, Path) in entries)
            {
                if (Path.ToLower().EndsWith(".dll") && IO.Exists(Path))
                    Entries.TryAdd(Path);
            }
        }
    }
}