namespace Cutulu.Core
{
    using System.Collections.Generic;

    public static class ArgumentDictionary
    {
        public static Dictionary<string, string> GetDictionary(this string[] _args, char _seperator = ':')
        {
            var _dictionary = new Dictionary<string, string>();

            if (_args.NotEmpty())
            {
                foreach (var _plain in _args)
                {
                    var _split = _plain.Split(_seperator, CONST.StringSplit);

                    if (_split.Size() >= 2)
                    {
                        _dictionary[_split[0].ToLower()] = _split[1].ToLower();
                    }

                    else
                    {
                        var key = _plain.ToLower().Trim();

                        if (_dictionary.ContainsKey(key) == false)
                            _dictionary[key] = string.Empty;
                    }
                }
            }

            return _dictionary;
        }

        public static bool TryGetInt(this Dictionary<string, string> _source, string _key, out int _value)
        {
            _value = 0;
            return _source.TryGetValue(_key, out _key) && int.TryParse(_key, out _value);
        }
    }
}