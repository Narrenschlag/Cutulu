namespace Cutulu.Core.AutoComplete;

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

/// <summary>
/// Lightweight text normalizer optimized for autocomplete search.
/// Provides efficient normalization without language-specific processing.
/// </summary>
public static class Normalizer
{
    private static readonly Regex _cleaner = new("[^a-z0-9 ]", RegexOptions.Compiled);
    private static readonly Regex _multiSpace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes input text to lowercase, removes diacritics, and cleans special characters.
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Convert to lowercase and handle common character replacements
        input = input.ToLowerInvariant()
            //.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")
            .Replace("ä", "a").Replace("ö", "o").Replace("ü", "u").Replace("ß", "s")
            .Replace("ae", "a").Replace("oe", "o").Replace("ue", "u")
            .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
            .Replace("á", "a").Replace("à", "a").Replace("â", "a")
            .Replace("ó", "o").Replace("ò", "o").Replace("ô", "o")
            .Replace("ú", "u").Replace("ù", "u").Replace("û", "u")
            .Replace("í", "i").Replace("ì", "i").Replace("î", "i");

        // Remove remaining diacritics
        input = RemoveDiacritics(input);

        // Remove non-alphanumeric characters except spaces
        input = _cleaner.Replace(input, " ");

        // Normalize multiple spaces to single space
        input = _multiSpace.Replace(input, " ");

        return input.Trim();
    }

    /// <summary>
    /// Splits normalized text into tokens, filtering out single characters.
    /// </summary>
    public static string[] Tokenize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        return input.Split(' ', CONST.StringSplit)
                   .Where(t => t.Length > 1)
                   .ToArray();
    }

    /// <summary>
    /// Enhanced tokenization that creates additional sub-tokens from longer words.
    /// Useful for compound terms without language-specific processing.
    /// </summary>
    public static string[] TokenizeEnhanced(string input)
    {
        var basicTokens = Tokenize(input);
        var allTokens = new HashSet<string>(basicTokens);

        // Add sub-tokens from longer words
        foreach (var token in basicTokens)
        {
            if (token.Length > 6)
            {
                var subTokens = CreateSubTokens(token);
                foreach (var subToken in subTokens)
                {
                    if (subToken.Length > 2)
                        allTokens.Add(subToken);
                }
            }
        }

        return [.. allTokens];
    }

    /// <summary>
    /// Removes diacritics (accents) from text using Unicode normalization.
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(text.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Creates sub-tokens from compound words using simple heuristics.
    /// Uses generic patterns without language-specific assumptions.
    /// </summary>
    private static List<string> CreateSubTokens(string word)
    {
        var parts = new List<string>();

        // Try to split at vowel-consonant boundaries (basic heuristic)
        for (int i = 3; i < word.Length - 3; i++)
        {
            if (IsVowel(word[i - 1]) && IsConsonant(word[i]) && IsConsonant(word[i + 1]))
            {
                var before = word[..i];
                var after = word[i..];

                if (before.Length > 2 && after.Length > 2)
                {
                    parts.Add(before);
                    parts.Add(after);
                    break;
                }
            }
        }

        return parts;
    }

    /// <summary>
    /// Simple vowel check for common vowels.
    /// </summary>
    private static bool IsVowel(char c) => "aeiouäöü".Contains(c);

    /// <summary>
    /// Simple consonant check - any letter that's not a vowel.
    /// </summary>
    private static bool IsConsonant(char c) => char.IsLetter(c) && !IsVowel(c);
}