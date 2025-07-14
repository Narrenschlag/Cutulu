namespace Cutulu.Core.AutoComplete;

using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

public static class Normalizer
{
    private static readonly Regex _cleaner = new("[^a-z0-9 ]", RegexOptions.Compiled);

    public static string Normalize(string input)
    {
        input = input.ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

        input = RemoveDiacritics(input);
        input = _cleaner.Replace(input, "");
        return input.Trim();
    }

    public static string[] Tokenize(string input) =>
        input.Split(' ', CONST.StringSplit);

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}