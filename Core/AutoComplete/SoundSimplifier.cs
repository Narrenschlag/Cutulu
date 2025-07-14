namespace Cutulu.Core.AutoComplete;

public static class SoundSimplifier
{
    public static string Simplify(string input)
    {
        input = Normalizer.Normalize(input);

        return input
            .Replace("ck", "k")
            .Replace("tz", "z")
            .Replace("ph", "f")
            .Replace("v", "f")
            .Replace("w", "v")
            .Replace("ig", "ich")
            .Replace("sch", "s")
            .Replace("sp", "s")
            .Replace("st", "s")
            .Replace("g", "k")
            .Replace("b", "p")
            .Replace("d", "t")
            .Replace("z", "s")
            .Replace("x", "ks");
    }
}
