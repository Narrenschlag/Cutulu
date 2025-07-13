namespace Cutulu.Core
{
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class AutocompleteItem
    {
        public string Key { get; set; }
        public string DisplayText { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Priority { get; set; } = 0;

        public override string ToString() => $"{DisplayText} ({Price:C}/{Unit})";
    }

    public class UsageRecord
    {
        public string Key { get; set; }
        public DateTime LastUsed { get; set; }
        public int TotalCount { get; set; }
        public List<DateTime> RecentUsages { get; set; } = new List<DateTime>();

        public int GetWeeklyCount()
        {
            var weekAgo = DateTime.Now.AddDays(-7);
            return RecentUsages.Count(u => u >= weekAgo);
        }

        public double GetTrendScore()
        {
            var weeklyCount = GetWeeklyCount();
            var daysSinceLastUse = (DateTime.Now - LastUsed).TotalDays;

            // Mehr Punkte für häufige Nutzung in letzter Woche
            var weeklyScore = weeklyCount * 10;

            // Weniger Punkte je länger her die letzte Nutzung
            var recencyScore = Math.Max(0, 30 - daysSinceLastUse * 2);

            return weeklyScore + recencyScore;
        }
    }

    public class AutocompleteResult
    {
        public AutocompleteItem Item { get; set; }
        public double Score { get; set; }
        public string MatchedOn { get; set; }
        public bool IsExactMatch { get; set; }
    }

    public class SmartAutocompleteHelper
    {
        private readonly Dictionary<string, AutocompleteItem> _items = new();
        private readonly Dictionary<string, List<string>> _shortcuts = new();
        private readonly Dictionary<string, List<string>> _synonyms = new();
        private readonly List<AutocompleteItem> _recentItems = new();
        private Dictionary<string, UsageRecord> _usageRecords = new();

        // Persistenz-Keys
        private const string USAGE_DATA_KEY = "autocomplete_usage_data";
        private const string RECENT_ITEMS_KEY = "autocomplete_recent_items";

        public int MaxResults { get; set; } = 10;
        public int MaxRecentItems { get; set; } = 5;
        public bool EnableFuzzySearch { get; set; } = true;
        public double FuzzyThreshold { get; set; } = 0.6;

        public SmartAutocompleteHelper()
        {
            LoadLearningData();
        }

        // Items hinzufügen
        public void AddItem(string key, string displayText, string description = "",
                           decimal price = 0, string unit = "Stk", int priority = 0)
        {
            var item = new AutocompleteItem
            {
                Key = key,
                DisplayText = displayText,
                Description = description,
                Price = price,
                Unit = unit,
                Priority = priority
            };

            _items[key.ToLower()] = item;
        }

        // Shortcuts hinzufügen
        public void AddShortcut(string shortcut, string targetKey)
        {
            var key = shortcut.ToLower();
            if (!_shortcuts.ContainsKey(key))
                _shortcuts[key] = new List<string>();

            _shortcuts[key].Add(targetKey.ToLower());
        }

        // Synonyme hinzufügen
        public void AddSynonym(string synonym, string targetKey)
        {
            var key = synonym.ToLower();
            if (!_synonyms.ContainsKey(key))
                _synonyms[key] = new List<string>();

            _synonyms[key].Add(targetKey.ToLower());
        }

        // Bulk-Setup für Baugewerbe
        public void SetupBaugewerbeDefaults()
        {
            // Schrauben
            AddItem("schraube_m4", "Schraube M4x20 Titan", "Titanschraube 4mm", 0.20m, "Stk", 5);
            AddItem("schraube_m6", "Schraube M6x30 Edelstahl", "Edelstahlschraube 6mm", 0.35m, "Stk", 5);
            AddItem("schraube_spax", "SPAX Universalschraube", "Gelb verzinkt 4x40", 0.15m, "Stk", 8);

            // Platten
            AddItem("rigips_standard", "Rigipsplatte Standard", "12,5mm Gipskarton", 2.50m, "m²", 10);
            AddItem("rigips_feuer", "Rigipsplatte Feuerschutz", "12,5mm F30", 4.20m, "m²", 7);
            AddItem("osb_platte", "OSB-Platte", "18mm wasserfest", 8.50m, "m²", 6);

            // Dämmstoffe
            AddItem("steinwolle", "Steinwolle-Dämmung", "160mm Klemmfilz", 12.80m, "m²", 4);
            AddItem("glaswolle", "Glaswolle-Dämmung", "120mm Klemmfilz", 8.90m, "m²", 4);

            // Shortcuts
            AddShortcut("M4", "schraube_m4");
            AddShortcut("M6", "schraube_m6");
            AddShortcut("S4", "schraube_m4");
            AddShortcut("SPAX", "schraube_spax");
            AddShortcut("R12", "rigips_standard");
            AddShortcut("RF", "rigips_feuer");
            AddShortcut("OSB", "osb_platte");
            AddShortcut("SW", "steinwolle");
            AddShortcut("GW", "glaswolle");

            // Synonyme
            AddSynonym("Rigips", "rigips_standard");
            AddSynonym("Gipskarton", "rigips_standard");
            AddSynonym("Gipsplatte", "rigips_standard");
            AddSynonym("Feuerschutz", "rigips_feuer");
            AddSynonym("Dämmung", "steinwolle");
            AddSynonym("Isolation", "steinwolle");
            AddSynonym("Titan", "schraube_m4");
            AddSynonym("Edelstahl", "schraube_m6");

            // Items nach dem Setup laden falls bereits vorhanden
            LoadRecentItemsFromKeys();
        }

        // Hauptsuche
        public List<AutocompleteResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetRecentItems();

            var results = new List<AutocompleteResult>();
            var processedQuery = query.ToLower().Trim();

            // 1. Exact Shortcut Match
            if (_shortcuts.ContainsKey(processedQuery))
            {
                foreach (var targetKey in _shortcuts[processedQuery])
                {
                    if (_items.ContainsKey(targetKey))
                    {
                        results.Add(new AutocompleteResult
                        {
                            Item = _items[targetKey],
                            Score = 1000 + _items[targetKey].Priority * 10 + GetTrendScore(targetKey),
                            MatchedOn = "Shortcut",
                            IsExactMatch = true
                        });
                    }
                }
            }

            // 2. Synonyme
            if (_synonyms.ContainsKey(processedQuery))
            {
                foreach (var targetKey in _synonyms[processedQuery])
                {
                    if (_items.ContainsKey(targetKey))
                    {
                        results.Add(new AutocompleteResult
                        {
                            Item = _items[targetKey],
                            Score = 900 + _items[targetKey].Priority * 10 + GetTrendScore(targetKey),
                            MatchedOn = "Synonym",
                            IsExactMatch = true
                        });
                    }
                }
            }

            // 3. Direkte Suche in Items
            foreach (var item in _items.Values)
            {
                var score = CalculateScore(processedQuery, item);
                if (score > 0)
                {
                    results.Add(new AutocompleteResult
                    {
                        Item = item,
                        Score = score + GetTrendScore(item.Key),
                        MatchedOn = "Direct",
                        IsExactMatch = item.DisplayText.ToLower().Contains(processedQuery)
                    });
                }
            }

            // 4. Fuzzy Search
            if (EnableFuzzySearch && results.Count < MaxResults)
            {
                foreach (var item in _items.Values)
                {
                    if (results.Any(r => r.Item.Key == item.Key)) continue;

                    var fuzzyScore = CalculateFuzzyScore(processedQuery, item);
                    if (fuzzyScore >= FuzzyThreshold)
                    {
                        results.Add(new AutocompleteResult
                        {
                            Item = item,
                            Score = fuzzyScore * 100 + item.Priority + GetTrendScore(item.Key),
                            MatchedOn = "Fuzzy",
                            IsExactMatch = false
                        });
                    }
                }
            }

            // Sortieren und begrenzen
            return results
                .OrderByDescending(r => r.Score)
                .Take(MaxResults)
                .ToList();
        }

        // Scoring-Algorithmus
        private double CalculateScore(string query, AutocompleteItem item)
        {
            var displayText = item.DisplayText.ToLower();
            var description = item.Description?.ToLower() ?? "";
            var tags = string.Join(" ", item.Tags).ToLower();
            var combinedText = $"{displayText} {description} {tags}";

            double score = 0;

            // Exact match am Anfang = höchste Punktzahl
            if (displayText.StartsWith(query))
                score += 800;
            else if (displayText.Contains(query))
                score += 600;
            else if (description.Contains(query))
                score += 400;
            else if (tags.Contains(query))
                score += 300;

            // Wort-Boundaries berücksichtigen
            if (Regex.IsMatch(combinedText, $@"\b{Regex.Escape(query)}\b"))
                score += 200;

            // Priorität hinzufügen
            score += item.Priority * 10;

            // Kürze bevorzugen bei ähnlichen Scores
            if (score > 0)
                score += (100 - displayText.Length) * 0.1;

            return score;
        }

        // Fuzzy Matching
        private double CalculateFuzzyScore(string query, AutocompleteItem item)
        {
            var displayText = item.DisplayText.ToLower();
            var distance = LevenshteinDistance(query, displayText);
            var maxLength = Math.Max(query.Length, displayText.Length);

            return maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;
        }

        // Levenshtein Distance
        private int LevenshteinDistance(string s1, string s2)
        {
            if (s1 == s2) return 0;
            if (s1.Length == 0) return s2.Length;
            if (s2.Length == 0) return s1.Length;

            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        // Recent Items für leere Suche
        private List<AutocompleteResult> GetRecentItems()
        {
            return _recentItems.Take(MaxRecentItems)
                .Select(item => new AutocompleteResult
                {
                    Item = item,
                    Score = 100 + GetTrendScore(item.Key),
                    MatchedOn = "Recent",
                    IsExactMatch = false
                })
                .ToList();
        }

        // Item wurde verwendet - mit Persistenz und Trend-Tracking
        public void RegisterUsage(string key)
        {
            key = key.ToLower();
            var now = DateTime.Now;

            if (!_usageRecords.ContainsKey(key))
            {
                _usageRecords[key] = new UsageRecord { Key = key };
            }

            var record = _usageRecords[key];
            record.LastUsed = now;
            record.TotalCount++;
            record.RecentUsages.Add(now);

            // Nur die letzten 14 Tage behalten
            var twoWeeksAgo = now.AddDays(-14);
            record.RecentUsages = record.RecentUsages.Where(u => u >= twoWeeksAgo).ToList();

            // Recent Items aktualisieren
            UpdateRecentItems(key);

            // Daten speichern
            SaveLearningData();
        }

        // Recent Items updaten
        private void UpdateRecentItems(string key)
        {
            if (_items.ContainsKey(key))
            {
                var item = _items[key];
                _recentItems.RemoveAll(r => r.Key == key);
                _recentItems.Insert(0, item);

                if (_recentItems.Count > MaxRecentItems)
                    _recentItems.RemoveAt(_recentItems.Count - 1);
            }
        }

        // Trend-Score basierend auf letzten 7 Tagen
        private double GetTrendScore(string key)
        {
            if (!_usageRecords.ContainsKey(key.ToLower()))
                return 0;

            return _usageRecords[key.ToLower()].GetTrendScore();
        }

        // Persistenz - Lerndaten speichern
        private void SaveLearningData()
        {
#if GODOT4_0_OR_GREATER
            try
            {
                USAGE_DATA_KEY.SetAppData(_usageRecords);
                RECENT_ITEMS_KEY.SetAppData(_recentItems.Select(i => i.Key).ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern der Lerndaten: {ex.Message}");
            }
#endif
        }

        // Persistenz - Lerndaten laden
        private void LoadLearningData()
        {
#if GODOT4_0_OR_GREATER
            try
            {
                _usageRecords = USAGE_DATA_KEY.GetAppData<Dictionary<string, UsageRecord>>() ?? new Dictionary<string, UsageRecord>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Lerndaten: {ex.Message}");
                _usageRecords = new Dictionary<string, UsageRecord>();
            }
#endif
        }

        // Recent Items aus gespeicherten Keys laden
        private void LoadRecentItemsFromKeys()
        {
#if GODOT4_0_OR_GREATER
            try
            {
                var recentKeys = RECENT_ITEMS_KEY.GetAppData<List<string>>() ?? new List<string>();
                _recentItems.Clear();

                foreach (var key in recentKeys)
                {
                    if (_items.ContainsKey(key))
                    {
                        _recentItems.Add(_items[key]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Recent Items: {ex.Message}");
            }
#endif
        }

        // Lerndaten zurücksetzen
        public void ResetLearningData()
        {
            _usageRecords.Clear();
            _recentItems.Clear();

#if GODOT4_0_OR_GREATER
            USAGE_DATA_KEY.RemoveAppData();
            RECENT_ITEMS_KEY.RemoveAppData();
#endif
        }

        // Statistiken
        public void PrintStats()
        {
            Console.WriteLine($"Items: {_items.Count}");
            Console.WriteLine($"Shortcuts: {_shortcuts.Count}");
            Console.WriteLine($"Synonyms: {_synonyms.Count}");
            Console.WriteLine($"Recent: {_recentItems.Count}");

            if (_usageRecords.Any())
            {
                var topUsed = _usageRecords
                    .OrderByDescending(x => x.Value.TotalCount)
                    .First();

                var topTrending = _usageRecords
                    .OrderByDescending(x => x.Value.GetWeeklyCount())
                    .First();

                Console.WriteLine($"Most used: {topUsed.Key}({topUsed.Value.TotalCount})");
                Console.WriteLine($"Trending: {topTrending.Key}({topTrending.Value.GetWeeklyCount()}/Woche)");
            }
        }

        // Detaillierte Analytics
        public Dictionary<string, object> GetAnalytics()
        {
            var analytics = new Dictionary<string, object>();

            analytics["total_items"] = _items.Count;
            analytics["total_usages"] = _usageRecords.Sum(r => r.Value.TotalCount);
            analytics["weekly_usage"] = _usageRecords.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetWeeklyCount());
            analytics["most_used"] = _usageRecords.OrderByDescending(r => r.Value.TotalCount).Take(5).ToDictionary(r => r.Key, r => r.Value.TotalCount);
            analytics["trending"] = _usageRecords.OrderByDescending(r => r.Value.GetWeeklyCount()).Take(5).ToDictionary(r => r.Key, r => r.Value.GetWeeklyCount());

            return analytics;
        }
    }
}