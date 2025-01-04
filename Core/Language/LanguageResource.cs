namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System;
    using Godot;

    [GlobalClass]
    public partial class LanguageResource : Resource
    {
        [Export] public string LanguageCode { get; set; } = "de";
        [Export] public string EnglishName { get; set; } = "German";
        [Export] public string NativeName { get; set; } = "Deutsch";
        [Export] public Texture2D Flag { get; set; }
        [Export(PropertyHint.MultilineText)] public string Content { get; set; } = "asset_title::Inhalts-Pakete";
    }
}