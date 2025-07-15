namespace Cutulu.Core.AutoComplete;

using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Please forgive me the german documentation. I'm writing this basically in my free time and felt more like documenting it this way today.
/// So basically this is a fuzzy-search engine for autocomplete suggestions. You insert your terms into the engine and it will return the best matches based on given terms.
/// Really proud of it. Works really fast and well for me. I'm using it in my applications relying on quick search results.
/// </summary>
public class AutoCompleteEngine
{
    private readonly Dictionary<string, UsageData> Usage = [];
    private readonly LevenshteinHelper Levenshtein = new();
    private readonly List<IndexedEntry> Items = [];

    public void LoadItems(IEnumerable<string> rawTerms, bool add = false)
    {
        // Handle index & Reset usage
        if (add == false)
        {
            Items.Clear();
            Reset();
        }

        foreach (var term in rawTerms)
            Items.Add(new(term));
    }

    public void LoadItems(IEnumerable<(string, object)> items, bool add = false)
    {
        // Handle index & Reset usage
        if (add == false)
        {
            Items.Clear();
            Reset();
        }

        // Handle index
        foreach (var item in items)
            Items.Add(new(item.Item1, item.Item2));
    }

    private void Reset()
    {
        Usage.Clear();

        foreach (var usage in LoadObject<List<UsageData>>() ?? [])
            Usage[usage.NormalizedName] = usage;
    }

    public void SaveUsage() => SaveObject(Usage.Values.ToList());

    public List<string> Search(string query, int maxResults = 10)
    {
        var normalizedQuery = Normalizer.Normalize(query);
        var queryTokens = Normalizer.Tokenize(normalizedQuery);

        var results = new List<ScoredResult>();

        foreach (var entry in Items)
        {
            var baseScore = ScoreMatch(entry, normalizedQuery, queryTokens);
            var usageScore = ScoreUsage(entry.Normalized);

            results.Add(new ScoredResult(entry.Original, baseScore + usageScore));
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .Select(r => r.Text)
            .ToList();
    }

    public void RecordSelection(string selectedText)
    {
        var normalized = Normalizer.Normalize(selectedText);
        if (!Usage.TryGetValue(normalized, out var data))
        {
            data = new UsageData(normalized);
            Usage[normalized] = data;
        }

        data.UseCount++;
        data.LastUsed = DateTime.UtcNow;
        data.SessionCount++;
    }

    public void ToggleFavorite(string text)
    {
        var normalized = Normalizer.Normalize(text);
        if (!Usage.TryGetValue(normalized, out var data))
        {
            data = new UsageData(normalized);
            Usage[normalized] = data;
        }

        data.IsManuallyFavorited = !data.IsManuallyFavorited;
    }

    private int ScoreMatch(IndexedEntry entry, string normQuery, string[] queryTokens)
    {
        var score = entry.Normalized.Contains(normQuery) ?
            entry.Normalized.StartsWith(normQuery) ? 100 :
            entry.Normalized.EndsWith(normQuery) ? 100 :
            75 : 0;

        // Abkürzungen
        if (entry.Abbreviation == normQuery) score += 80;

        // Token-Übereinstimmung
        {
            var tokenHits = queryTokens.Count(t => entry.Tokens.Contains(t));
            score += tokenHits * 15;
        }

        // Kombinierter LCS + Levenshtein-Score für Tippfehler UND Reihenfolge-Toleranz
        {
            var lcs = LongestCommonSubsequenceLength(normQuery, entry.Normalized);
            //if (lcs >= normQuery.Length * 0.8) // mind. 80% der Query sind subsequenz
            //    score += lcs * 10; // -> Alter Score

            var lev = Levenshtein.Distance(normQuery, entry.Normalized);
            // score += Math.Max(0, 60 - dist * 10); -> Alter Score

            var lcsRatio = (float)lcs / normQuery.Length;
            var levPenalty = lev * 0.5f; // Gewichtung Tippfehler halbieren

            var combinedScore = (int)(lcsRatio * 100 - levPenalty * 10);

            // Bei sehr kurzen Eingaben ggf. Score halbieren, da Fehler sonst zu stark ins Gewicht fallen
            if (normQuery.Length < 4) combinedScore /= 2;

            if (combinedScore > 0) score += combinedScore;
        }

        // Phonetische Ähnlichkeit
        {
            var queryPhon = SoundSimplifier.Simplify(normQuery);

            if (queryPhon == entry.Phonetic)
            {
                score += 70;
            }

            else if (entry.Phonetic.StartsWith(queryPhon))
            {
                score += 40; // phonetisches Präfix-Matching
            }

            else
            {
                var pDist = Levenshtein.Distance(queryPhon, entry.Phonetic);
                score += Math.Max(0, 40 - pDist * 10); // tolerant bis Distanz ~3
            }
        }

        // BITAP fuzzy match für fehlende Buchstaben
        if (BitapMatcher.IsMatch(entry.Normalized, normQuery))
            score += 60;

        // Wildcard-Levenshtein mit '*' als Auslasszeichen
        if (normQuery.Length >= 4 && normQuery.Length <= entry.Normalized.Length)
        {
            var wildcardQuery = InsertWildcards(normQuery);
            var dist = WildcardLevenshtein.Distance(wildcardQuery, entry.Normalized);
            score += Math.Max(0, 40 - dist * 10);
        }

        return score;
    }

    private int ScoreUsage(string normalizedName)
    {
        if (!Usage.TryGetValue(normalizedName, out var usage)) return 0;

        int score = 0;
        if (usage.IsManuallyFavorited) score += 100;

        int hours = (int)(DateTime.UtcNow - usage.LastUsed).TotalHours;
        score += Math.Max(0, 50 - hours); // Decay over ~2 days

        score += Math.Min(usage.UseCount, 50); // Cap usage influence
        score += Math.Min(usage.SessionCount * 3, 30); // More sessions = more trusted

        return score;
    }

    private static int LongestCommonSubsequenceLength(string a, string b)
    {
        var dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                if (a[i - 1] == b[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }
        return dp[a.Length, b.Length];
    }

    private static string InsertWildcards(string input)
    {
        if (input.Length < 4) return input;

        var chars = input.ToCharArray();
        var mid = input.Length / 2;
        chars[mid] = '*'; // ersetzt z. B. Mitte durch „Fehlzeichen“

        return new string(chars);
    }

    private void SaveObject<T>(T _) { } // leave for user to implement
    private T? LoadObject<T>() where T : class => null;
}