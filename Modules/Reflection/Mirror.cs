using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace Cutulu
{
    public static class Mirror
    {
        #region Types        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns all classes and types that derive from the given type and are contained in the given namespace.
        /// </summary>
        public static IEnumerable<Type> GetAllTypesOf(this string @namespace, Type derivesFrom)
        => from t in Assembly.GetExecutingAssembly().GetTypes() where t.IsClass && t.IsSubclassOf(derivesFrom) && t.Namespace == @namespace select t;

        /// <summary>
		/// Returns all classes and types that derive from the given type in its namespace.
		/// </summary>
        public static Type[] GetAllTypesOf(this Type derivesFrom)
        => GetAllTypesOf(derivesFrom.Namespace, derivesFrom).ToArray();
        #endregion

        #region Invoking     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Invoke a method in the class using a type reference.
        /// </summary>
        public static T Invoke<T>(this Type @class, string methodName, params object[] @params)
        {
            // Validate if @class is actually a class
            if (@class.IsClass == false)
            {
                Debug.LogError($"typeof({@class}) has to be a class");
                return default;
            }

            // Get static build method in type
            MethodInfo method = @class.GetMethod(methodName);

            #region Method Validation
            // Validate that method is not generic
            if (method.IsGenericMethod == true)
            {
                Debug.LogError($"typeof({@class}) is generic, but you did not assign any generics");
                return default;
            }
            #endregion

            #region Parameters Validation
            // Mismatching parameters
            var methodParams = method.GetParameters();
            if (methodParams == null != (@params == null) || (@params != null && methodParams.Length != @params.Length))
            {
                Debug.LogError($"Parameter count mismatch (method: {(methodParams != null ? methodParams.Length : 0)} != {(@params != null ? @params.Length : 0)}) for method({methodName}) in typeof({@class}).");
                return default;
            }
            #endregion

            // Get result of the method
            object result = method.Invoke(null, @params);

            // Return result as T
            return result != null && result is T t ? t : default;
        }

        /// <summary>
		/// Invoke a generic method in the class using a type reference.
		/// </summary>
        public static T Invoke<T>(this Type @class, string methodName, Type[] generics, params object[] @params)
        {
            // Validate if @class is actually a class
            if (@class.IsClass == false)
            {
                Debug.LogError($"typeof({@class}) has to be a class.");
                return default;
            }

            // Get static build method in type
            MethodInfo method = @class.GetMethod(methodName);

            #region Method Validation
            // Validate that method is not generic
            if (method.IsGenericMethod == false)
            {
                // Return basic Invoke result
                if (generics == null || generics.Length < 1)
                {
                    return Invoke<T>(@class, methodName, @params);
                }

                Debug.LogError($"typeof({@class}) is not generic, but you are trying to use generics.");
                return default;
            }
            #endregion

            #region Generics Validation
            // Trying to run generic function without any generic types
            else if (generics == null || generics.Length < 1)
            {
                Debug.LogError($"You are trying to call a generic method without any generics defined.");
                return default;
            }

            // Generic type amount is mismatching
            else if (generics.Length != method.GetGenericArguments().Length)
            {
                Debug.LogError($"Your generics count is mismatching for method({methodName}) in typeof({@class}).");
                return default;
            }
            #endregion

            #region Parameters Validation
            // Mismatching parameters
            var methodParams = method.GetParameters();
            if (methodParams == null != (@params == null) || (@params != null && methodParams.Length != @params.Length))
            {
                Debug.LogError($"Parameter count mismatch (method: {(methodParams != null ? methodParams.Length : 0)} != {(@params != null ? @params.Length : 0)}) for method({methodName}) in typeof({@class}).");
                return default;
            }
            #endregion

            // Create generic method based on type
            method = method.MakeGenericMethod(generics);

            // Get result of the method
            object result = method.Invoke(null, @params);

            // Return result as T
            return result != null && result is T t ? t : default;
        }
        #endregion

        #region Parameters   ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
		/// Mirror all values of the same class/struct type to another.
        /// <br/>Only mirrors values declared as { public get; } and/or { public set; }.
		/// </summary>
        public static void MirrorTo<T>(this T source, ref T target)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                target.SetParameterValue(properties[i].Name, source.GetParameterValue<T, object>(properties[i].Name));
            }
        }

        /// <summary>
		/// Set value of class/struct to given value.
        /// <br/>Only writes values declared as { public get; } and/or { public set; }.
		/// </summary>
        public static void SetParameterValue<T, V>(this T t, string valueName, V value)
            => typeof(T).GetProperty(valueName).SetValue(t, value, null);

        /// <summary>
		/// Get value of class/struct
        /// <br/>Only reads values declared as { public get; } and/or { public set; }.
		/// </summary>
        public static V GetParameterValue<T, V>(this T t, string valueName)
        {
            object value = typeof(T).GetProperty(valueName).GetValue(t, null);

            return value.Equals(default(V)) ? default : (V)value;
        }
        #endregion
    }
}