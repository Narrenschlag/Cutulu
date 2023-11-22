using System.Collections.Generic;
using System.Text;

namespace Cutulu
{
    public class NestedString
    {
        public List<NestedString> Children { get; set; }
        public string Value { get; set; }

        public NestedString Parent { get; set; }
        public string Key { get; set; }
        public int Depth { get; set; }

        public int StartX { get; set; }
        public int LastX { get; set; }

        public bool emptyValue() => Value.IsEmpty();

        public NestedString(string Key, int Start, int Last, NestedString parent = null, int depth = 0)
        {
            Children = new List<NestedString>();
            this.Key = Key;

            StartX = Start;
            LastX = Last;

            Parent = parent;
            Value = "";

            if (depth < 0) Depth = Parent.Depth + 1;
            else Depth = depth;
        }

        #region Object Parsing
        // Just a concept, as this is actually not made as JSON alternative but basically for my dialogue system
        private static T Parse<T>(string plain, string[] openKeys, string closeKey = "$", char openSuffix = ':', char closePrefix = ':', bool clipDepth = true, bool trimValues = true) where T : class, new()
        {
            NestedString nestedString = Parse(plain, openKeys, closeKey, openSuffix, closePrefix, trimValues);
            T _class = new T();

            List<(string key, string value)> list = new List<(string key, string value)>();
            foreach ((string key, string value) in list)
                _class.SetFieldValue(key, value);

            return _class;
        }
        #endregion

        #region Parsing
        public static NestedString Parse(string plain, string openKey, string closeKey = "$", char openSuffix = ':', char closePrefix = ':', bool clipDepth = true, bool trimValues = true)
            => Parse(plain, new string[1] { openKey }, closeKey, openSuffix, closePrefix, clipDepth, trimValues);

        public static NestedString Parse(string plain, string[] openKeys = null, string closeKey = "$", char openSuffix = ':', char closePrefix = ':', bool clipDepth = true, bool trimValues = true)
        {
            // Fix keys (remove emties, trim existing)
            if (openKeys.NotEmpty())
            {
                List<string> _keys = new List<string>();
                for (int i = openKeys.Length - 1; i >= 0; i--)
                {
                    if (openKeys[i].NotEmpty()) _keys.Add(openKeys[i].Trim());
                }

                openKeys = _keys.ToArray();
            }

            // Assign default key
            if (openKeys.IsEmpty()) openKeys = new string[1] { "$" };

            // Define result value to be given back
            NestedString result = new NestedString("", 0, plain.Length - 1);

            // Variable values
            NestedString current = result;
            int depth = 0, last = 0;

            // Algorithm for full text
            for (int i = 0; i < plain.Length;)
            {
                // Open nested
                if (opens(i, out string key))
                {
                    add(last, i - last);

                    i += key.Length + 1;
                    int start = last;
                    last = i;

                    deeper(key, start, last);
                }

                // Close nested
                else if (closes(i))
                {
                    add(last, i - last);

                    i += closeKey.Length + 1;
                    last = i;

                    up(ref i);
                }

                else i++;
            }

            // Add last
            if (last < plain.Length)
                add(last, plain.Length - last);

            // Fix depth
            if (clipDepth) result.FixDepth();

            // Trim values
            if (trimValues) result.TrimValues();

            // Return value
            return result;

            void add(int start, int length)
            {
                current.Value += plain.Substring(start, length);
            }

            #region Depth
            void deeper(string key, int start, int last)
            {
                NestedString nested = new NestedString(key, start, last, current, depth + 1);

                current.Children.Add(nested);
                current = nested;

                depth++;
            }

            void up(ref int i)
            {
                if (current.Parent == null) $"Depth out of range at char {i}".Throw();

                if (current.Value.Trim().IsEmpty()) current.Value = null;
                if (current.Children.IsEmpty()) current.Children = null;

                bool isEmpty = current.Children == null && current.Value == null;
                current = current.Parent;

                if (isEmpty) current.Children.RemoveAt(current.Children.Count - 1);

                depth--;
            }
            #endregion

            #region Detection
            bool opens(int i, out string _key)
            {
                for (int k = 0; k < openKeys.Length; k++)
                    if (_opens(openKeys[k], out _key))
                        return true;

                _key = null;
                return false;

                bool _opens(string str, out string k)
                {
                    k = str;

                    if (plain.Length - (i + 1) < str.Length + 1) return false;
                    if (!plain[i + str.Length].Equals(openSuffix)) return false;

                    return plain.Substring(i, str.Length).StartsWith(str);
                }
            }

            bool closes(int i)
            {
                if (!plain[i].Equals(closePrefix)) return false;
                if (plain.Length - (i + 1) < closeKey.Length) return false;

                return plain.Substring(i + 1, closeKey.Length).Equals(closeKey);
            }
            #endregion
        }
        #endregion

        #region String
        public string ToString(string closeKey = "$", char openSuffix = ':', char closePrefix = ':') => ToString(this, closeKey, openSuffix, closePrefix);
        public static string ToString(NestedString nestedString, string closeKey = "$", char openSuffix = ':', char closePrefix = ':')
        {
            // Define string builder for performance
            StringBuilder stringBuilder = new StringBuilder();
            loop(nestedString, false);

            // Recursive loop
            void loop(NestedString nestedString, bool appendDetection = true)
            {
                // Take care of open suffix/key
                if (appendDetection) stringBuilder.Append($"{nestedString.Key}{openSuffix}{nestedString.Value}");

                // Ignore for first layer
                else stringBuilder.Append(nestedString.Value);

                // Handle children
                if (nestedString.Children.NotEmpty())
                    for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                    {
                        loop(nestedString.Children[i]);
                    }

                // Take care of close prefix/key
                if (appendDetection) stringBuilder.Append($"{closePrefix}{closeKey}");
            }

            // Return built string
            return stringBuilder.ToString();
        }
        #endregion

        #region Extensions
        public void FixDepth() => FixDepth(this);
        public static void FixDepth(NestedString nestedString)
        {
            // Remove empty and replace their child's parent
            if (nestedString.emptyValue() && nestedString.Parent != null)
            {
                for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                {
                    // Define child as parent's child and alter depth
                    nestedString.Parent.Children.Add(nestedString.Children[i]);
                    nestedString.Children[i].Parent = nestedString.Parent;
                    nestedString.Children[i].Depth--;
                }

                // Remove this from parent
                nestedString.Parent.Children.Remove(nestedString);
            }

            // Continue loop
            if (nestedString.Children.NotEmpty())
                for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                    FixDepth(nestedString.Children[i]);
        }

        public void TrimValues() => TrimValues(this);
        public static void TrimValues(NestedString nestedString)
        {
            // Trim value
            if (nestedString.Value.NotEmpty())
                nestedString.Value = nestedString.Value.Trim();

            // Continue loop
            if (nestedString.Children.NotEmpty())
                for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                    TrimValues(nestedString.Children[i]);
        }

        public void PrintString() => PrintString(this);
        public static void PrintString(NestedString nestedString)
        {
            if (nestedString.Value.NotEmpty())
                $"{depth()}{nestedString.Value.TrimEnd()}  -> {nestedString.Key}".Log();

            if (nestedString.Children.NotEmpty())
                for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                {
                    PrintString(nestedString.Children[i]);
                }

            string depth()
            {
                string str = "";
                for (int i = 0; i < nestedString.Depth; i++)
                {
                    str += '.';
                }
                return str;
            }
        }

        public string[] ReadValues(string key) => ReadValues(this, key);
        public static string[] ReadValues(NestedString nestedString, string key)
        {
            List<string> values = new List<string>();
            key = key.Trim();

            loop(nestedString, ref values);
            return values.IsEmpty() ? null : values.ToArray();

            void loop(NestedString nestedString, ref List<string> values)
            {
                if (nestedString.Key.Equals(key))
                    values.Add(nestedString.Value);

                if (nestedString.Children.NotEmpty())
                    for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                        loop(nestedString.Children[i], ref values);
            }
        }

        public (string value, int start, int last)[] ReadValues2(string key) => ReadValues2(this, key);
        public static (string value, int start, int last)[] ReadValues2(NestedString nestedString, string key)
        {
            List<(string value, int start, int last)> values = new List<(string value, int start, int last)>();
            key = key.Trim();

            loop(nestedString, ref values);
            return values.IsEmpty() ? null : values.ToArray();

            void loop(NestedString nestedString, ref List<(string value, int start, int last)> values)
            {
                if (nestedString.Key.Equals(key))
                    values.Add((nestedString.Value, nestedString.StartX, nestedString.LastX));

                if (nestedString.Children.NotEmpty())
                    for (int i = nestedString.Children.Count - 1; i >= 0; i--)
                        loop(nestedString.Children[i], ref values);
            }
        }
        #endregion
    }
}