namespace Cutulu
{
    using System.Collections.Generic;

    public static class AppData
    {
        public static readonly Dictionary<string, object> Bin = new();

        public const string Path = $"{IO.USER_PATH}ApplicationData/";
        public const string End = ".appData";

        private static string GetPath(string key) => $"{Path}{key}{End}";

        public static T GetAppData<T>(this string key, T defaultValue = default)
        {
            return TryGetAppData(key, out T output) ? output : defaultValue;
        }

        public static bool TryGetAppData<T>(this string key, out T output)
        {
            if (Bin.TryGetValue(key, out var obj) && obj is T t)
            {
                output = t;
                return true;
            }

            else if (ContainsAppData(key))
            {
                IO.TryRead(GetPath(key), out output, IO.FileType.Binary);
                Bin[key] = output;

                return true;
            }

            output = default;
            return false;
        }

        public static void SetAppData(this string key, object obj)
        {
            if (obj == null)
            {
                RemoveAppData(key);
                Bin.TryRemove(key);
                return;
            }

            else
            {
                IO.Write(obj, GetPath(key), IO.FileType.Binary);
                Bin[key] = obj;
            }
        }

        public static bool TrySetAppData(this string key, object obj)
        {
            if (ContainsAppData(key) == false)
            {
                IO.Write(obj, GetPath(key), IO.FileType.Binary);
                Bin[key] = obj;
                return true;
            }

            return false;
        }

        public static void RemoveAppData(this string key)
        {
            if (ContainsAppData(key))
            {
                IO.DeleteFile(GetPath(key));
            }
        }

        public static bool ContainsAppData(this string key)
        {
            return IO.Exists(GetPath(key));
        }
    }
}