namespace Cutulu
{
    public static class AppData
    {
        public const string Path = $"{IO.USER_PATH}ApplicationData/";
        public const string End = ".appData";

        private static string GetPath(string key) => $"{Path}{key}{End}";

        public static T GetAppData<T>(this string key, T defaultValue = default)
        {
            return TryGetAppData(key, out T output) ? output : defaultValue;
        }

        public static bool TryGetAppData<T>(this string key, out T output)
        {
            if (ContainsAppData(key))
            {
                return IO.TryRead(GetPath(key), out output, IO.FileType.Binary);
            }

            output = default;
            return false;
        }

        public static void SetAppData(this string key, object obj)
        {
            if (obj == null)
            {
                RemoveAppData(key);
                return;
            }

            else
            {
                IO.Write(obj, GetPath(key), IO.FileType.Binary);
            }
        }

        public static bool TrySetAppData(this string key, object obj)
        {
            if (ContainsAppData(key) == false)
            {
                IO.Write(obj, GetPath(key), IO.FileType.Binary);
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