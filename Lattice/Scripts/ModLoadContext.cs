namespace Cutulu.Lattice
{
    using System.Runtime.Loader;
    using System.Reflection;

    public class ModLoadContext : AssemblyLoadContext
    {
        public ModLoadContext() : base(isCollectible: true) { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Return null to let the default context handle core assemblies
            return null;
        }
    }
}