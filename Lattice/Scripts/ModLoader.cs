namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public static class ModLoader
    {
        public static List<IMod> LoadFromDirectory(string path)
        {
            var mods = new List<IMod>();
            var files = System.IO.Directory.GetFiles(path, "*.dll");

            foreach (var file in files)
            {
                var assembly = System.Reflection.Assembly.LoadFile(file);
                var modTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(IMod)));

                foreach (var modType in modTypes)
                {
                    if (Activator.CreateInstance(modType) is IMod mod)
                    {
                        mods.Add(mod);
                    }
                }
            }

            return mods;
        }
    }
}