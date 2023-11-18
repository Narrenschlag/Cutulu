using System.Collections.Generic;
using Godot;

namespace Cutulu
{
    public static class Modding
    {
        private static Dictionary<char, FilePortal> _portals;
        public static Dictionary<char, FilePortal> Portals
        {
            get
            {
                if (_portals == null) setup();
                return _portals;
            }
        }

        public static FilePortal Assets => Portals['A'];
        public static FilePortal Mods => Portals['M'];
        private static void setup()
        {
            _portals = new Dictionary<char, FilePortal>();

            Register('M', IO.PROJECT_PATH + "Mods/");
            Register('A', IO.PROJECT_PATH + "Assets/");
        }

        public static FilePortal Custom(char key)
            => TryGetCustom(key, out var portal) ? portal : default;

        public static bool TryGetCustom(char key, out FilePortal portal)
            => Portals.TryGetValue(key, out portal);

        public static void Register(this char key, string path)
        {
            if (Portals.ContainsKey(key)) return;
            Portals.Add(key, new FilePortal(path));
        }

        public static List<FilePortal> Find(this string localFile) => TryFind(localFile, out var portals) ? portals : default;
        public static bool TryFind(this string localFile, out List<FilePortal> list, string mask = null)
        {
            var _list = new List<FilePortal>();

            // First consider mods...
            if (mask.IsEmpty() || mask.Contains('M'))
                loop('M');

            // ...then assets
            if (mask.IsEmpty() || mask.Contains('A'))
                loop('A');

            foreach (char c in Portals.Keys)
            {
                if (c.Equals('M') || c.Equals('A')) continue;

                loop(c);
            }

            void loop(char c)
            {
                if (mask.NotEmpty() && !mask.Contains(c))
                    return;

                if (Portals[c].Contains(localFile))
                    _list.Add(Portals[c]);
            }

            list = _list.IsEmpty() ? null : _list;
            return list.NotEmpty();
        }

        public struct FilePortal
        {
            public string DefaultPathSuffix;
            public string PathPrefix;

            public bool Valid => !Equals(default);

            #region Construction
            public FilePortal() : this(IO.USER_PATH) { }
            public FilePortal(string path, string defaultEnding = ".txt")
            {
                PathPrefix = path.EndsWith('/') ? path : path + '/';
                DefaultPathSuffix = defaultEnding;
            }
            #endregion

            #region Foundation
            private string fix(ref string localPath)
            {
                if (!(localPath = localPath.Trim()).Contains('.')) localPath += DefaultPathSuffix;
                if (!localPath.StartsWith(PathPrefix)) localPath = PathPrefix + localPath;
                return localPath;
            }

            public void Write(string localPath, string text, string encryptionKey = null)
            {
                IO.WriteText(fix(ref localPath), text, encryptionKey);
            }

            public string Read(string localPath, string encryptionKey = null)
                => TryRead(localPath, out string text, encryptionKey) ? text : default;
            public bool TryRead(string localPath, out string text, string encryptionKey = null)
                => IO.TryLoadTxt(fix(ref localPath), out text, encryptionKey);
            #endregion

            #region Advanced
            public T ReadResource<T>(string localPath) where T : class
                => TryReadResource(localPath, out T result) ? result : default;
            public bool TryReadResource<T>(string localPath, out T result) where T : class
                => IO.TryLoad(fix(ref localPath), out result);

            public T ReadJson<T>(string localPath)
                => TryReadJson(localPath, out T result) ? result : default;
            public bool TryReadJson<T>(string localPath, out T result)
            {
                bool success = TryRead(fix(ref localPath), out string json);

                result = json.json<T>();
                return success;
            }

            public void WriteJson<T>(string localPath, T value, string encryptionKey = null)
            {
                if (value.Equals(default)) Write(fix(ref localPath), "", encryptionKey);
                else Write(fix(ref localPath), value.json(), encryptionKey);
            }
            #endregion

            #region Utility
            public string[] ReadFileRelatives(string localPath)
                => DirAccess.GetFilesAt(fix(ref localPath));

            public bool Contains(string localPath)
                => IO.Exists(fix(ref localPath));
            #endregion
        }
    }
}