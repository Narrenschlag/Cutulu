namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;

    public static class Lang
    {
        public static readonly Dictionary<string, string> Base = new(), Additional = new();
        public static LanguageResource Current { get; private set; }
        public static Action Updated { get; set; }

        public static string CurrentLanguageCode => Current.NotNull() ? Current.LanguageCode : default;

        public static void SetTo(LanguageResource lang)
        {
            if (Current == lang) return;

            OverrideLang(Base, Current = lang, true);
            Updated?.Invoke();
        }

        public static void Add(LanguageResource lang, bool clearOld = false)
        {
            if (Current == lang) return;

            OverrideLang(Additional, lang, clearOld);
            Updated?.Invoke();
        }

        public static string Get(string key, string defaultValue = default)
        {
            if (key.IsEmpty()) return defaultValue.IsEmpty() ? string.Empty : defaultValue;

            if (Base.TryGetValue(key, out var value) || Additional.TryGetValue(key, out value)) return value;
            else return defaultValue.IsEmpty() ? key : defaultValue;
        }

        private static void OverrideLang(Dictionary<string, string> dictionary, LanguageResource lang, bool clear)
        {
            if (clear) dictionary.Clear();

            if (lang.IsNull() || lang.Content.IsEmpty()) return;

            var split = lang.Content.Split(new[] { "\n" }, Constant.StringSplit);
            var builder = new System.Text.StringBuilder();
            var key = default(string);

            for (var i = 0; i < split.Length; i++)
            {
                var arr = split[i].Split(new[] { "::" }, Constant.StringSplit);

                if (arr.Length != 2)
                {
                    if (key.NotEmpty()) builder.AppendLine(split[i]);
                }

                else
                {
                    set();

                    key = arr[0];
                    builder.AppendLine(arr[1]);
                }
            }

            set();
            Updated?.Invoke();

            void set()
            {
                if (builder.Length > 0 && key.NotEmpty())
                {
                    Base[key] = builder.ToString();

                    builder.Clear();
                }
            }
        }
    }
}