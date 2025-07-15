namespace Cutulu.Core.AutoComplete;

using System.Linq;

public readonly struct IndexedEntry
{
    public string Original { get; }
    public object Key { get; }

    public string Normalized { get; }
    public string[] Tokens { get; }
    public string Abbreviation { get; }
    public string Phonetic { get; }

    public IndexedEntry(string original, object key = null)
    {
        Original = original;
        Key = key;

        Normalized = Normalizer.Normalize(original);
        Tokens = Normalizer.Tokenize(Normalized);
        Abbreviation = string.Concat(Tokens.Select(t => t.Length > 0 ? t[0] : '_'));
        Phonetic = SoundSimplifier.Simplify(Normalized);
    }
}
