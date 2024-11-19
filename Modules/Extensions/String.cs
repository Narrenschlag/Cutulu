namespace Cutulu
{
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;

    public static class StringExtension
    {
        public static string RemoveForbiddenDbChars(this string source) => RemoveChar(source, ' ', '#', '\'', '`', '\'', '@', '/', '\\');

        /// <summary>
        /// Returns string without listed char values
        /// </summary>
        public static string RemoveChar(this string source, params char[] chars)
        {
            if (chars.IsEmpty()) return source;

            ICollection<char> list = chars;
            return new((
                    from c in source where !list.Contains(c) select c
                ).ToArray());
        }

        /// <summary>
        /// Returns only listed char values in string
        /// </summary>
        public static string KeepChar(this string source, params char[] chars)
        {
            if (chars.IsEmpty()) return source;

            ICollection<char> list = chars;
            return new((
                    from c in source where list.Contains(c) select c
                ).ToArray());
        }

        public static string TrimEndUntil(this string str, params char[] ids)
        {
            if (ids.IsEmpty()) return str;

            var splits = str.Split(ids, Constants.StringSplit);
            if (splits.Size() < 2) return str;
            return str[..^splits[^1].Length];
        }

        /// <summary>
        /// Splits string after first instance of seperator
        /// </summary>
        public static string[] SplitOnce(this string source, params char[] serperators)
        {
            if (serperators.IsEmpty() || source.IsEmpty()) return null;

            for (int i = 0; i < serperators.Length; i++)
            {
                // Found a char
                if (source.Contains(serperators[i]))
                {
                    break;
                }

                // No char has been found
                if (i >= serperators.Length - 1)
                {
                    return null;
                }
            }

            // Find split index
            ICollection<char> list = serperators;
            for (int i = 0; i < source.Length - 1; i++)
            {
                if (list.Contains(source[i]))
                {
                    // Define before and after
                    string before = source[..i].Trim();
                    string after = source[(i + 1)..].Trim();

                    // Validate before and after
                    if (before.NotEmpty() && after.NotEmpty())
                    {
                        return new string[2] { before, after };
                    }

                    // Before or after is empty
                    else break;
                }
            }

            return null;
        }

        /// <summary>
        /// Trims spaces to avoid double and more spaces
        /// </summary>
        public static string TrimSpaces(this string source)
        {
            StringBuilder result = new();
            source = source.Trim();

            bool wasSpace = false;
            bool isSpace;

            // Enumerate through source
            for (int i = 0; i < source.Length; i++)
            {
                // Check if is space
                isSpace = source[i] == ' ';

                // Check if was space before
                if (isSpace == false || wasSpace == false)
                {
                    result.Append(source[i]);
                }

                // Set past to present
                wasSpace = isSpace;
            }

            return result.ToString();
        }

        /// <summary>
        /// Splits string into lines and executes function that returns the modified line
        /// </summary>
        public static string[] SplitAnd(this string source, char seperator, Func<string, string> actionPerLine, bool ignoreEmptyEntries = false)
        {
            string[] splits = source.Split(seperator, Constants.StringSplit);
            List<string> lines = new();

            for (int i = 0; i < splits.Length; i++)
            {
                if (ignoreEmptyEntries)
                {
                    string line = actionPerLine.Invoke(splits[i]);
                    if (line.NotEmpty())
                    {
                        lines.Add(line);
                    }
                }

                else lines.Add(actionPerLine.Invoke(splits[i]));
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Insert value in front of keys
        /// </summary>
        public static string InsertInFrontOf(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();

            ICollection<char> splits = keys;
            for (ushort i = 0; i < source.Length; i++)
            {
                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                }

                stringBuilder.Append(source[i]);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Insert value in front of and after keys
        /// </summary>
        public static string InsertInFrontOfAndAfter(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();

            ICollection<char> splits = keys;
            for (ushort i = 0; i < source.Length; i++)
            {
                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                    stringBuilder.Append(source[i]);
                    stringBuilder.Append(insertValue);
                }

                else stringBuilder.Append(source[i]);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Inserts value after keys
        /// </summary>
        public static string InsertAfter(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();
            stringBuilder.Append(source[0]);

            ICollection<char> splits = keys;
            for (ushort i = 1; i < source.Length; i++)
            {
                stringBuilder.Append(source[i]);

                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Removes empty lines
        /// </summary>
        public static string RemoveEmptyLines(this string source)
        {
            if (source.IsEmpty()) return source;

            string[] lines = source.Split('\n', Constants.StringSplit);
            StringBuilder stringBuilder = new();

            stringBuilder.Append(lines[0]);
            for (ushort i = 1; i < lines.Length; i++)
            {
                stringBuilder.Append($"\n{lines[i]}");
            }

            return stringBuilder.ToString();
        }

        public static string RemoveBehind(this string source, string identifier)
        {
            if (identifier.IsEmpty() || source.IsEmpty() || source.Contains(identifier[0]) == false || source.Contains(identifier) == false) return source;

            return source.Split(identifier, Constants.StringSplit)[0];
        }

        public static string TrimToDirectory(this string path) => TrimToDirectory(path, new[] { '\\', '/' });
        public static string TrimToDirectory(this string path, params char[] chars)
        {
            if (path.IsEmpty() || chars.IsEmpty()) return path;

            var contains = false;
            for (int i = 0; i < chars.Length; i++)
            {
                if (path.Contains(chars[i]) == false) continue;

                contains = true;
                break;
            }

            return contains ? path.TrimEndUntil(chars) : path;
        }

        public static string ReplaceFirst(this string str, char c, object value)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!str[i].Equals(c)) continue;

                str = str[..i++] + value + str[i..];
                break;
            }

            return str;
        }

        public static string Fill(this string str, int targetLength, char fillChar, bool before = true)
        {
            if (targetLength < 1 || (str = str.Trim()).Length >= targetLength) return str;

            StringBuilder builder = new();
            builder.Append(str);

            for (int i = builder.Length; i < targetLength; i++)
                if (before) builder.Insert(0, fillChar);
                else builder.Append(fillChar);

            return builder.ToString();
        }

        public static bool IsEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool NotEmpty(this string str) => !IsEmpty(str);

        // Kind of splits up a string to only write down the contents between the signals. Nice for a lot of stuff.
        public static string Extract(this string source, char signal, out string[] extracted, bool removeSignal = true) => Extract(source, new List<char>() { signal }, out extracted, removeSignal);
        public static string Extract(this string source, List<char> signals, out string[] extracted, bool removeSignals = true)
        {
            extracted = null;

            if (source.IsEmpty()) return source;

            StringBuilder result = new();

            List<string> _extracted = new();
            StringBuilder str = new();
            bool active = false;

            for (int c = 0; c < source.Length; c++)
            {
                if (signals.Contains(source[c]))
                {
                    active = !active;

                    if (!active && !removeSignals) str.Append(source[c]);

                    if (str.ToString().NotEmpty())
                        _extracted.Add(str.ToString());
                    str.Clear();

                    if (active && !removeSignals) str.Append(source[c]);
                }
                else
                {
                    if (active) str.Append(source[c]);
                    else result.Append(source[c]);
                }
            }

            if (str.ToString().NotEmpty()) _extracted.Add(str.ToString());

            extracted = _extracted.ToArray();
            return result.ToString();
        }

        public static bool IsEmail(this string mail)
        {
            if (mail.IsEmpty()) return false;
            if (!mail.Contains('@')) return false;
            if (mail.EndsWith('.') || mail.EndsWith('@')) return false;

            string[] splits = mail.Split('@');
            if (splits.Length != 2) return false;
            if (!splits[1].Contains('.')) return false;

            return true;
        }
    }
}