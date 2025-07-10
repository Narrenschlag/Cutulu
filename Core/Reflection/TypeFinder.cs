namespace Cutulu.Core.Reflection
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;

    public partial class TypeFinder
    {
        public readonly Dictionary<Type, HashSet<Type>> Types = [];

        private static readonly Dictionary<Assembly, Type[]> AssemblyTypeCache = [];
        private readonly HashSet<Type> ProcessedTypes = [];

        public const FLAGS DefaultFlags = FLAGS.CLASS_ONLY;

        public TypeFinder() { }

        public TypeFinder(Type refType, FLAGS flags = DefaultFlags, Assembly assembly = null) : this()
        {
            FindTypes(refType, flags, assembly);
        }

        public void FindTypes(Type refType, FLAGS flags = DefaultFlags, Assembly assembly = null)
        {
            // Type has already been added to Types
            if (Types.TryAdd(refType, []) == false) return;

            assembly ??= Assembly.GetExecutingAssembly();
            var types = GetCachedTypes(assembly);

            var abstractAllowed = flags.HasFlag(FLAGS.ABSTRACT_ALLOWED);
            var genericOnly = flags.HasFlag(FLAGS.GENERIC_ONLY);
            var classOnly = flags.HasFlag(FLAGS.CLASS_ONLY);

            foreach (var curType in types)
            {
                if (ProcessedTypes.Contains(curType)) continue;

                if ((classOnly && curType.IsClass == false) || (abstractAllowed == false && curType.IsAbstract)) continue;

                if (!genericOnly && refType.IsAssignableFrom(curType))
                {
                    Types[refType].Add(curType);
                    continue;
                }

                var baseType = curType.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericDef = baseType.GetGenericTypeDefinition();
                        if (refType.IsAssignableFrom(genericDef))
                        {
                            Types[refType].Add(curType);
                            break;
                        }
                    }

                    baseType = baseType.BaseType;
                }
            }
        }

        public void Clear()
        {
            Types.Clear();
            ProcessedTypes.Clear();
        }

        private static Type[] GetCachedTypes(Assembly assembly)
        {
            if (AssemblyTypeCache.TryGetValue(assembly, out var types) == false)
                AssemblyTypeCache[assembly] = types = assembly.GetTypes();

            return types;
        }

        [Flags]
        public enum FLAGS : byte
        {
            NONE = 0,
            CLASS_ONLY = 1,
            GENERIC_ONLY = 2,
            ABSTRACT_ALLOWED = 4,
        }
    }
}