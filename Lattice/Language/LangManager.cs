#if GODOT4_0_OR_GREATER
namespace Cutulu.Lattice
{
    using System.Collections.Generic;

    using Core;

    public static class LangManager
    {
        public static readonly Dictionary<string, string> Values = new();
        public static readonly List<string> Collections = new();

        public static event System.Action UpdatedLanguage;

        public const string DefaultLangCode = "en";

        private static string langCode;
        public static string LangCode
        {
            get => langCode;

            set
            {
                value = value.Trim().ToLower();
                if (LangCode == value) return;
                langCode = value;

                Reload();
            }
        }

        public static void Load(string collectionName, bool overwriteLoadOrder = true) => Load(new[] { collectionName }, overwriteLoadOrder, true);
        public static void Load(string[] collectionNames, bool overwriteLoadOrder = true, bool callUpdate = true)
        {
            if (collectionNames.IsEmpty()) return;

            var updated = false;
            foreach (var collectionName in collectionNames)
            {
                if (Collections.Contains(collectionName) && overwriteLoadOrder == false) continue;

                Collections.Add(collectionName);

                var parsed = collectionName.EndsWith('/') ?
                LangParser.Parse($"{collectionNames}{LangCode}") :
                LangParser.Parse($"{collectionNames}/{LangCode}");

                foreach (var key in parsed.Keys)
                {
                    Values[key] = parsed[key];
                }

                if (parsed.Count > 0) updated = true;
            }

            if (updated && callUpdate) UpdatedLanguage?.Invoke();
        }

        public static void Reload()
        {
            if (Collections.IsEmpty()) return;

            Values.Clear();

            var collections = Collections.ToArray();
            Load(collections, true, false);

            UpdatedLanguage?.Invoke();
        }
    }

    public static class LangManagerExtension
    {
        public static string Lang(this string key, string defaultValue = default)
        {
            if (key.NotEmpty())
            {
                if (key[0] == LangParser.KeyChar) key = key[1..];

                if (LangManager.Values.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return defaultValue.IsEmpty() ? key : defaultValue;
        }
    }
}
#endif