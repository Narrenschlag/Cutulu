namespace Cutulu.Core.AutoComplete;

using System.Linq;
using System.Collections.Generic;

public readonly struct IndexedEntry
{
    public string Original { get; }
    public object Key { get; }
    public string Normalized { get; }
    public string[] Tokens { get; }
    public string Abbreviation { get; }
    public string Phonetic { get; }
    public HashSet<string> NGrams { get; } // NEW: Pre-computed n-grams for faster lookup
    public string[] WordStarts { get; } // NEW: First few characters of each word

    public IndexedEntry(string original, object key = null)
    {
        Original = original;
        Key = key;
        Normalized = Normalizer.Normalize(original);
        Tokens = Normalizer.Tokenize(Normalized);
        Abbreviation = string.Concat(Tokens.Select(t => t.Length > 0 ? t[0] : '_'));
        Phonetic = SoundSimplifier.Simplify(Normalized);

        // Pre-compute n-grams for faster searching
        NGrams = GenerateNGrams(Normalized, 2, 3);

        // Pre-compute word starts for better prefix matching
        WordStarts = Tokens.Select(t => t.Length >= 2 ? t.Substring(0, 2) : t).ToArray();
    }

    private static HashSet<string> GenerateNGrams(string text, int minN, int maxN)
    {
        var ngrams = new HashSet<string>();

        for (int n = minN; n <= System.Math.Min(maxN, text.Length); n++)
        {
            for (int i = 0; i <= text.Length - n; i++)
            {
                ngrams.Add(text.Substring(i, n));
            }
        }

        return ngrams;
    }
}