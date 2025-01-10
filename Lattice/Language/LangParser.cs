namespace Cutulu.Lattice
{
    using System.Collections.Generic;
    
    using Core;

    public static class LangParser
    {
        public const char KeyChar = '§';

        public static Dictionary<string, string> Parse(string name)
        {
            var dict = new Dictionary<string, string>();

            if (AssetLoader.TryGet(name, out string plainText))
            {
                var lines = plainText.Split('\n', System.StringSplitOptions.TrimEntries);
                var value = "";
                var key = "";

                foreach (var line in lines)
                {
                    if (line.StartsWith(KeyChar))
                    {
                        var args = line.Split(' ', 2, Cutulu.Core.Constant.StringSplit);

                        // Assign value to dictionary
                        if (key.NotEmpty() && value.NotEmpty())
                        {
                            dict[key] = value.Trim();
                        }

                        if (args.Length > 1)
                        {
                            key = args[0].Substring(1);
                            value = args[1];
                        }

                        else
                        {
                            key = line.Substring(1);
                            value = "";
                        }
                    }

                    else value += $"\n{line}";
                }
            }

            return dict;
        }
    }
}