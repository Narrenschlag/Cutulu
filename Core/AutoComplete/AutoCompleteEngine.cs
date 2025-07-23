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
    private readonly HashSet<IndexedEntry> Items = [];

    public void LoadItems(IEnumerable<string> rawTerms, bool add = false)
    {
        // Handle index & Reset usage
        if (add == false) Clear();

        foreach (var term in rawTerms)
            Items.Add(new(term));
    }

    public void LoadItems<KEY>(IEnumerable<string> rawTerms, KEY key, bool add = false)
    {
        // Handle index & Reset usage
        if (add == false) Clear();

        foreach (var term in rawTerms)
            Items.Add(new(term, key));
    }

    public void LoadItems<KEY>(IEnumerable<KeyValuePair<string, KEY>> items, bool add = false)
    {
        // Handle index & Reset usage
        if (add == false) Clear();

        // Handle index
        foreach (var (Text, Key) in items)
            Items.Add(new(Text, Key));
    }

    public void Clear()
    {
        Items.Clear();
        Reset();
    }

    private void Reset()
    {
        Usage.Clear();

        foreach (var usage in LoadObject<List<UsageData>>() ?? [])
            Usage[usage.NormalizedName] = usage;
    }

    public void SaveUsage() => SaveObject(Usage.Values.ToList());

    public List<ScoredResult> Search(string query, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalizedQuery = Normalizer.Normalize(query);
        var queryTokens = Normalizer.Tokenize(normalizedQuery);

        var results = new List<ScoredResult>();

        // Pre-filter for very short queries to improve performance
        var minScoreThreshold = normalizedQuery.Length <= 2 ? 20 : 10;

        foreach (var entry in Items)
        {
            var baseScore = ScoreMatch(entry, normalizedQuery, queryTokens);

            // Skip entries with very low scores to improve performance
            if (baseScore < minScoreThreshold)
                continue;

            var usageScore = ScoreUsage(entry.Normalized);
            var totalScore = baseScore + usageScore;

            results.Add(new ScoredResult(entry.Original, totalScore, entry.Key));
        }

        // Enhanced sorting with score decay for very long lists
        return results
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Text.Length) // Prefer shorter matches when scores are equal
            .Take(maxResults)
            .ToList();
    }

    // NEW: Batch search for better performance when searching multiple queries
    public Dictionary<string, List<ScoredResult>> BatchSearch(IEnumerable<string> queries, int maxResults = 10)
    {
        var results = new Dictionary<string, List<ScoredResult>>();

        foreach (var query in queries)
        {
            results[query] = Search(query, maxResults);
        }

        return results;
    }

    public List<string> SearchString(string query, int maxResults = 10)
    {
        return Search(query, maxResults)
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
        int score = 0;

        // 1. N-gram scoring (NEW) - handles scattered character matches like "grl" in "Heiliger Gral"
        {
            var ngramScore = CalculateNGramScore(normQuery, entry.Normalized);
            score += ngramScore;
        }

        // 2. Subsequence scoring with position bonus (IMPROVED)
        {
            var subseqScore = CalculateSubsequenceScore(normQuery, entry.Normalized);
            score += subseqScore;
        }

        // 3. Exact substring matches (OPTIMIZED)
        {
            if (entry.Normalized.Contains(normQuery))
            {
                if (entry.Normalized.StartsWith(normQuery))
                    score += 120; // Higher bonus for prefix matches
                else if (entry.Normalized.EndsWith(normQuery))
                    score += 100;
                else
                {
                    // Position-based scoring for mid-string matches
                    var pos = entry.Normalized.IndexOf(normQuery);
                    var positionBonus = Math.Max(0, 50 - pos * 2); // Earlier = better
                    score += 75 + positionBonus;
                }
            }
        }

        // 4. Abbreviation matching (ENHANCED)
        {
            if (entry.Abbreviation == normQuery)
                score += 100;
            else if (entry.Abbreviation.StartsWith(normQuery))
                score += 80;
            else if (CalculatePartialAbbreviationScore(normQuery, entry.Abbreviation) > 0)
                score += CalculatePartialAbbreviationScore(normQuery, entry.Abbreviation);
        }

        // 5. Token matching (EXISTING - keeps good performance)
        {
            var tokenHits = queryTokens.Count(t => entry.Tokens.Contains(t));
            score += tokenHits * 20; // Slightly increased
        }

        // 6. Improved Levenshtein with adaptive thresholds (OPTIMIZED)
        {
            var lev = Levenshtein.Distance(normQuery, entry.Normalized);
            var maxLen = Math.Max(normQuery.Length, entry.Normalized.Length);
            var similarity = 1.0 - (double)lev / maxLen;

            if (similarity > 0.3) // Only count if reasonably similar
            {
                var levScore = (int)(similarity * 60);
                score += levScore;
            }
        }

        // 7. Phonetic matching (EXISTING)
        {
            var queryPhon = SoundSimplifier.Simplify(normQuery);

            if (queryPhon == entry.Phonetic)
                score += 70;
            else if (entry.Phonetic.StartsWith(queryPhon))
                score += 40;
            else
            {
                var pDist = Levenshtein.Distance(queryPhon, entry.Phonetic);
                score += Math.Max(0, 40 - pDist * 10);
            }
        }

        // 8. BITAP fuzzy match (EXISTING)
        if (BitapMatcher.IsMatch(entry.Normalized, normQuery))
            score += 60;

        // 9. Word boundary bonus (NEW)
        {
            var boundaryScore = CalculateWordBoundaryScore(normQuery, entry.Normalized);
            score += boundaryScore;
        }

        return score;
    }

    // NEW: N-gram scoring for scattered character matches
    private int CalculateNGramScore(string query, string target)
    {
        if (query.Length < 2) return 0;

        var score = 0;
        var queryGrams = GenerateNGrams(query, 2, 3);
        var targetGrams = GenerateNGrams(target, 2, 3);

        foreach (var qGram in queryGrams)
        {
            if (targetGrams.Contains(qGram))
            {
                score += qGram.Length == 2 ? 15 : 25; // Longer n-grams worth more
            }
        }

        return Math.Min(score, 80); // Cap to prevent dominating other factors
    }

    // NEW: Enhanced subsequence scoring with position awareness
    private int CalculateSubsequenceScore(string query, string target)
    {
        var matches = 0;
        var lastMatchPos = -1;
        var positionBonus = 0;

        foreach (var c in query)
        {
            var pos = target.IndexOf(c, lastMatchPos + 1);
            if (pos != -1)
            {
                matches++;
                // Bonus for characters appearing in order with minimal gaps
                if (lastMatchPos != -1 && pos - lastMatchPos <= 3)
                    positionBonus += 5;
                lastMatchPos = pos;
            }
        }

        if (matches == 0) return 0;

        var ratio = (double)matches / query.Length;
        var baseScore = (int)(ratio * 50);

        return baseScore + positionBonus;
    }

    // NEW: Word boundary scoring
    private int CalculateWordBoundaryScore(string query, string target)
    {
        var score = 0;
        var words = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.StartsWith(query))
                score += 30;
            else if (query.All(c => word.Contains(c)))
                score += 15;
        }

        return Math.Min(score, 45); // Cap the bonus
    }

    // NEW: Partial abbreviation matching
    private int CalculatePartialAbbreviationScore(string query, string abbreviation)
    {
        if (query.Length > abbreviation.Length) return 0;

        var matches = 0;
        var abbrevIndex = 0;

        foreach (var c in query)
        {
            while (abbrevIndex < abbreviation.Length && abbreviation[abbrevIndex] != c)
                abbrevIndex++;

            if (abbrevIndex < abbreviation.Length)
            {
                matches++;
                abbrevIndex++;
            }
        }

        var ratio = (double)matches / query.Length;
        return ratio >= 0.5 ? (int)(ratio * 40) : 0;
    }

    // Helper method for n-gram generation
    private HashSet<string> GenerateNGrams(string text, int minN, int maxN)
    {
        var ngrams = new HashSet<string>();

        for (int n = minN; n <= Math.Min(maxN, text.Length); n++)
        {
            for (int i = 0; i <= text.Length - n; i++)
            {
                ngrams.Add(text.Substring(i, n));
            }
        }

        return ngrams;
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

    private void SaveObject<T>(T _) => AppData.SetAppData("text/autocomplete.pref", _);

#if GODOT4_0_OR_GREATER
    private static T LoadObject<T>() where T : class => AppData.GetAppData<T>("text/autocomplete.pref");
#else
    private static T? LoadObject<T>() where T : class => AppData.GetAppData<T>("text/autocomplete.pref");
#endif
}