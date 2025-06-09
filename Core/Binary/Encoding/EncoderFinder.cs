namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using System;

    #region Encoders [3/3]
    public static class EncoderFinder
    {
        // Method to find all classes inheriting from BinaryEncoder<T>
        public static void FindEncoders(Assembly assembly, out List<Type> encoderTypes, out List<Type> genericTypes)
        {
            genericTypes = [];
            encoderTypes = [];

            // Get all types in the assembly
            var allTypes = assembly.GetTypes();

            foreach (var type in allTypes)
            {
                if (type.IsClass && type.IsAbstract == false)
                {
                    // Check if the type is a subclass of BinaryEncoder<T>
                    var baseTypes = GetBaseTypes(type);

                    foreach (var baseType in baseTypes)
                    {
                        if (baseType.IsGenericType &&
                            baseType.GetGenericTypeDefinition() == typeof(BinaryEncoder<>))
                        {
                            encoderTypes.Add(type);
                            break;
                        }

                        else if (baseType == typeof(GenericBinaryEncoder))
                        {
                            genericTypes.Add(type);
                            break;
                        }
                    }
                }
            }
        }

        // Helper method to get base types including generic base types
        private static List<Type> GetBaseTypes(Type type)
        {
            var baseTypes = new List<Type>();

            var currentBase = type.BaseType;
            while (currentBase != null)
            {
                baseTypes.Add(currentBase);
                currentBase = currentBase.BaseType;
            }

            return baseTypes;
        }
    }

    #endregion
}