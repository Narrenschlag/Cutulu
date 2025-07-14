namespace Cutulu.Core.AutoComplete;

using System.Collections.Generic;

public static class BitapMatcher
{
    public static bool IsMatch(string text, string pattern, int maxErrors = 2)
    {
        int m = pattern.Length;
        if (m == 0) return true;
        if (m > 31) return false; // Bitap max safe length (int)

        var R = new int[maxErrors + 1];
        for (int k = 0; k <= maxErrors; k++)
            R[k] = ~1;

        var patternMask = new Dictionary<char, int>();
        for (int i = 0; i < m; i++)
        {
            if (!patternMask.ContainsKey(pattern[i]))
                patternMask[pattern[i]] = 0;
            patternMask[pattern[i]] |= 1 << i;
        }

        foreach (char c in text)
        {
            int charMask = patternMask.ContainsKey(c) ? patternMask[c] : 0;

            int prevRk1 = R[0];
            R[0] = (R[0] << 1 | 1) & charMask;

            for (int k = 1; k <= maxErrors; k++)
            {
                int tmp = R[k];
                R[k] = ((R[k] << 1) | 1) & charMask;
                R[k] |= (prevRk1 << 1) | (prevRk1) | R[k - 1];
                prevRk1 = tmp;
            }

            if ((R[maxErrors] & (1 << (m - 1))) == 0)
                return true;
        }

        return false;
    }
}