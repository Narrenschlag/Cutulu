namespace Cutulu.Core
{
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;

    /// <summary>
    /// Helper class to build and use readable and beginner-friendly regex patterns.
    /// Supports named groups, repetitions, literals, alternations, and multiline inputs.
    /// </summary>
    public class RegexBuilder
    {
        private readonly StringBuilder _cache = new();

        public RegexBuilder StartOfLine()
        {
            _cache.Append("^");
            return this;
        }

        public RegexBuilder EndOfLine()
        {
            _cache.Append("$");
            return this;
        }

        public RegexBuilder ThenLiteral(string text, bool escape = true)
        {
            _cache.Append(escape ? Regex.Escape(text) : text);
            return this;
        }

        public RegexBuilder ThenWhitespace() => Add("\\s");
        public RegexBuilder ThenAnyCharacter() => Add(".");
        public RegexBuilder ThenDigit() => Add("\\d");
        public RegexBuilder ThenWordChar() => Add("\\w");
        public RegexBuilder ThenTab() => Add("\\t");
        public RegexBuilder ThenLineBreak() => Add("\\n");

        public RegexBuilder ThenDigits(int min = 1, int? max = null)
        {
            _cache.Append("\\d");
            AddQuantifier(min, max);
            return this;
        }

        public RegexBuilder ThenDecimal()
        {
            return ThenDigits(1, 3).ThenOneOf(",.").Until(r => r.Or(r => r.ThenWhitespace(), r => r.ThenLineBreak()));
        }

        public RegexBuilder ThenLettersOrDigits(int min = 1, int? max = null)
        {
            _cache.Append("\\w");
            AddQuantifier(min, max);
            return this;
        }

        public RegexBuilder ThenOneOf(string chars)
        {
            _cache.Append($"[{Regex.Escape(chars)}]");
            return this;
        }

        public RegexBuilder ThenNoneOf(string chars)
        {
            _cache.Append($"[^{Regex.Escape(chars)}]");
            return this;
        }

        /// <summary>
        /// Matches any character (including none) until the given literal char is found (non-greedy).
        /// Does NOT consume the delimiter char.
        /// Example: builder.Until('@') matches everything before '@' in 'abc@xyz'.
        /// </summary>
        public RegexBuilder Until(char delimiter)
        {
            // Use [^delimiter]*?  = any chars except delimiter, lazy
            _cache.Append($"[^{Regex.Escape(delimiter.ToString())}]*?");
            return this;
        }

        /// <summary>
        /// Matches any character (including none) until the given literal string is found (non-greedy).
        /// Does NOT consume the delimiter string.
        /// Example: builder.Until(\".com\") matches everything before '.com'.
        /// </summary>
        public RegexBuilder Until(string delimiter)
        {
            // We build a non-greedy pattern: .*?(?=delimiter)
            // where (?=...) is positive lookahead that does NOT consume delimiter
            _cache.Append($".*?(?={Regex.Escape(delimiter)})");
            return this;
        }

        /// <summary>
        /// Matches any character (including none) until a char NOT in given chars is found (non-greedy).
        /// Useful to match sequences made only of given chars.
        /// </summary>
        public RegexBuilder UntilNot(params char[] allowedChars)
        {
            // Create negated char class: [^allowedChars]*?
            var charsEscaped = new string(allowedChars).Replace("-", "\\-"); // escape dash if present
            _cache.Append($"[^{Regex.Escape(charsEscaped)}]*?");
            return this;
        }

        /// <summary>
        /// Matches any characters lazily until the specified stopping pattern appears (without consuming it).
        /// The stopping pattern is defined by the builder action, which builds a regex fragment.
        /// Example usage:
        ///   .Until(r => r.ThenDigits(1, null).ThenLiteral(\".\").ThenDigits(1, null))
        /// </summary>
        public RegexBuilder Until(Action<RegexBuilder> stopPatternBuilder)
        {
            // Build the stopping pattern separately:
            var stopPatternBuilderInstance = new RegexBuilder();
            stopPatternBuilder(stopPatternBuilderInstance);
            var stopPattern = stopPatternBuilderInstance._cache.ToString();

            // Append a lazy match of any char up to the stopping pattern (positive lookahead)
            _cache.Append($".*?(?={stopPattern})");
            return this;
        }

        /// <summary>
        /// Adds a non-capturing group of alternatives, each defined by a RegexBuilder action.
        /// Example:
        ///    .Or(
        ///       r => r.ThenLiteral("cat"),
        ///       r => r.ThenLiteral("dog").ThenWhitespace().ThenLiteral("bark"))
        /// </summary>
        public RegexBuilder Or(params Action<RegexBuilder>[] options)
        {
            _cache.Append("(?:");
            for (int i = 0; i < options.Length; i++)
            {
                if (i > 0)
                    _cache.Append("|");

                var optionBuilder = new RegexBuilder();
                options[i](optionBuilder);
                _cache.Append(optionBuilder._cache);
            }
            _cache.Append(")");
            return this;
        }

        public RegexBuilder Optional(Action<RegexBuilder> content)
        {
            _cache.Append("(?:");
            var inner = new RegexBuilder();
            content(inner);
            _cache.Append(inner._cache);
            _cache.Append(")?");
            return this;
        }

        public RegexBuilder Repeat(int min, int? max = null, Action<RegexBuilder> content = null)
        {
            _cache.Append("(?:");
            var inner = new RegexBuilder();
            content?.Invoke(inner);
            _cache.Append(inner._cache);
            _cache.Append(max == null ? $"{{{min},}})" : $"{{{min},{max}}})");
            return this;
        }

        public RegexBuilder Or(params string[] options)
        {
            _cache.Append("(?:");
            for (int i = 0; i < options.Length; i++)
            {
                if (i > 0) _cache.Append("|");
                _cache.Append(Regex.Escape(options[i]));
            }
            _cache.Append(")");
            return this;
        }

        public RegexBuilder Group(Action<RegexBuilder> content)
        {
            _cache.Append("(");
            var inner = new RegexBuilder();
            content(inner);
            _cache.Append(inner._cache);
            _cache.Append(")");
            return this;
        }

        public RegexBuilder OpenGroup(string name, Action<RegexBuilder> content)
        {
            _cache.Append($"(?<{name}>");
            var inner = new RegexBuilder();
            content(inner);
            _cache.Append(inner._cache);
            _cache.Append(")");
            return this;
        }

        public override string ToString() => _cache.ToString();

        private RegexBuilder Add(string raw)
        {
            _cache.Append(raw);
            return this;
        }

        private void AddQuantifier(int min, int? max)
        {
            if (min == 1 && max == null) return;
            if (min == 0 && max == null) _cache.Append("*");
            else if (min == 1 && max == null) _cache.Append("+");
            else if (min == 0 && max == 1) _cache.Append("?");
            else if (max == null) _cache.Append($"{{{min},}}");
            else _cache.Append($"{{{min},{max}}}");
        }

        /// <summary>
        /// Exports the built regex pattern as a Regex object with Multiline enabled by default.
        /// </summary>
        public Regex Export(RegexOptions options = RegexOptions.None)
            => new(_cache.ToString(), options | RegexOptions.Multiline | RegexOptions.Singleline);

        /// <summary>
        /// Applies the regex to the input string and returns extracted named groups.
        /// </summary>
        public bool Apply(string plainText, out Dictionary<string, string>[] matchGroups)
        {
            var result = new List<Dictionary<string, string>>();
            var regex = Export();

            var matches = regex.Matches(plainText);

            foreach (Match match in matches.Cast<Match>())
            {
                if (!match.Success) continue;

                var groupDict = new Dictionary<string, string>();
                foreach (string groupName in regex.GetGroupNames())
                {
                    if (int.TryParse(groupName, out _)) continue;

                    var group = match.Groups[groupName];

                    if (group.Success) groupDict[groupName] = group.Value;
                }

                if (groupDict.Count > 0) result.Add(groupDict);
            }

            return (matchGroups = [.. result]).Length > 0;
        }

        /// <summary>
        /// Applies the regex to the input string and returns extracted named groups of first occurance.
        /// </summary>
        public bool Apply(string plainText, out Dictionary<string, string> matchGroups)
        {
            var regex = Export();
            matchGroups = [];

            var match = regex.Match(plainText);

            if (match?.Success ?? false)
            {
                foreach (string groupName in regex.GetGroupNames())
                {
                    if (int.TryParse(groupName, out _)) continue;

                    var group = match.Groups[groupName];
                    if (group.Success) matchGroups[groupName] = group.Value;
                }
            }

            return matchGroups.Count > 0;
        }
    }
}