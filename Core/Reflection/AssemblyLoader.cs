namespace Cutulu.Core
{
    using System.Runtime.Loader;
    using System.Reflection;
    using System.Linq;
    using System;

    public static class AssemblyLoader
    {
        public static Assembly LoadAssembly(this AssemblyLoadContext context, string filePath)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);
            var loadedAssembly = context.LoadFromAssemblyPath(filePath);

            return loadedAssembly ?? Assembly.LoadFile(filePath);
        }

        public static Assembly LoadAssembly(string filePath)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));

            return loadedAssembly ?? Assembly.LoadFile(filePath);
        }

        public static Assembly GetAssembly(string filePath)
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);

            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)) ??
                null;
        }
    }
}