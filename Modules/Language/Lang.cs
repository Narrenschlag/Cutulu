namespace Cutulu
{
    using System.Collections.Generic;
    using System;
    using Godot;

    [GlobalClass]
    public partial class Lang : Resource
    {
        [Export] public string LanguageCode { get; set; } = "de";
        [Export] public string EnglishName { get; set; } = "German";
        [Export] public string NativeName { get; set; } = "Deutsch";
        [Export] public Texture2D Flag { get; set; }
        [Export(PropertyHint.MultilineText)] public string Content { get; set; } = "asset_title::Inhalts-Pakete";

        public static readonly Dictionary<string, string> Base = new(), Additional = new();
        public static Lang Current { get; private set; }
        public static Action Updated { get; set; }

        public static string CurrentLanguageCode => Current.NotNull() ? Current.LanguageCode : default;

        public static void SetTo(Lang lang)
        {
            if (Current == lang) return;

            OverrideLang(Base, Current = lang, true);
            Updated?.Invoke();
        }

        public static void Add(Lang lang, bool clearOld = false)
        {
            if (Current == lang) return;

            OverrideLang(Additional, lang, clearOld);
            Updated?.Invoke();
        }

        public static string Get(string key, string defaultValue = default)
        {
            if (Base.TryGetValue(key, out var value) || Additional.TryGetValue(key, out value)) return value;
            else return defaultValue.IsEmpty() ? key : defaultValue;
        }

        private static void OverrideLang(Dictionary<string, string> dictionary, Lang lang, bool clear)
        {
            if (clear) dictionary.Clear();

            if (lang.IsNull() || lang.Content.IsEmpty()) return;

            var split = lang.Content.Split(new[] { "\n" }, Constants.StringSplit);
            var builder = new System.Text.StringBuilder();
            var key = default(string);

            for (var i = 0; i < split.Length; i++)
            {
                var arr = split[i].Split(new[] { "::" }, Constants.StringSplit);

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