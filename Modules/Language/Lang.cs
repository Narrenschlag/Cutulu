namespace Cutulu
{
    using System.Collections.Generic;
    using System;
    using Cutulu;
    using Godot;

    [GlobalClass]
    public partial class Lang : Resource
    {
        [Export] public string LanguageCode { get; set; } = "de";
        [Export] public string EnglishName { get; set; } = "German";
        [Export] public string NativeName { get; set; } = "Deutsch";
        [Export] public Texture2D Flag { get; set; }
        [Export(PropertyHint.MultilineText)] public string Content { get; set; } = "asset_title::Inhalts-Pakete";

        public static readonly Dictionary<string, string> Dictionary = new();
        public static Lang Current { get; private set; }
        public static Action Updated { get; set; }

        public static string CurrentLanguageCode => Current.NotNull() ? Current.LanguageCode : default;

        public static void SetTo(Lang lang)
        {
            if (Current == lang || (Current = lang).Content.IsEmpty()) return;

            var split = Current.Content.Split(new[] { "\n" }, Constants.StringSplit);
            var builder = new System.Text.StringBuilder();
            var key = default(string);

            Dictionary.Clear();
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
                    Dictionary[key] = builder.ToString();

                    builder.Clear();
                }
            }
        }

        public static string Get(string key, string defaultValue = default)
        {
            if (Dictionary.TryGetValue(key, out var value)) return value;
            else return defaultValue.IsEmpty() ? key : defaultValue;
        }
    }
}