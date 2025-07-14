namespace Cutulu.Core.AutoComplete;

using System.Linq;

public class IndexedEntry
{
    public string Original { get; }
    public string Normalized { get; }
    public string[] Tokens { get; }
    public string Abbreviation { get; }
    public string Phonetic { get; }

    public IndexedEntry(string original)
    {
        Original = original;
        Normalized = Normalizer.Normalize(original);
        Tokens = Normalizer.Tokenize(Normalized);
        Abbreviation = string.Concat(Tokens.Select(t => t.Length > 0 ? t[0] : '_'));
        Phonetic = SoundSimplifier.Simplify(Normalized);
    }
}
