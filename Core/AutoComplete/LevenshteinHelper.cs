namespace Cutulu.Core.AutoComplete;

using System.Collections.Generic;
using System;

public class LevenshteinHelper
{
    private int[] _costs = new int[128];

    public int Distance(string a, string b)
    {
        if (a.Length > _costs.Length)
            Array.Resize(ref _costs, a.Length + 1);

        for (int i = 0; i <= a.Length; i++)
            _costs[i] = i;

        for (int j = 1; j <= b.Length; j++)
        {
            int prev = _costs[0]++;
            for (int i = 1; i <= a.Length; i++)
            {
                int insertCost = _costs[i] + 1;
                int deleteCost = prev + 1;

                int replaceCost = _costs[i - 1] + (ArePhoneticallySimilar(a[i - 1], b[j - 1]) ? 0 : 1); // Maybe change 0 to 0.5 for phonetic similarity

                prev = _costs[i];
                _costs[i] = Math.Min(Math.Min(insertCost, deleteCost), replaceCost);
            }
        }
        return _costs[a.Length];
    }

    private static readonly Dictionary<char, HashSet<char>> PhoneticNeighbors = new()
    {
        ['b'] = new HashSet<char> { 'p' },
        ['p'] = new HashSet<char> { 'b' },
        ['g'] = new HashSet<char> { 'k' },
        ['k'] = new HashSet<char> { 'g', 'c', 'q' },
        ['d'] = new HashSet<char> { 't' },
        ['t'] = new HashSet<char> { 'd' },
        ['f'] = new HashSet<char> { 'v', 'w' },
        ['v'] = new HashSet<char> { 'f', 'w' },
        ['s'] = new HashSet<char> { 'z', 'ß', 'c' },
        ['z'] = new HashSet<char> { 's', 'c' },
        ['ß'] = new HashSet<char> { 's' },
        ['c'] = new HashSet<char> { 'k', 's', 'z' },
        ['m'] = new HashSet<char> { 'n' },
        ['n'] = new HashSet<char> { 'm' }
    };

    private static bool ArePhoneticallySimilar(char a, char b)
    {
        if (a == b) return true;
        return PhoneticNeighbors.TryGetValue(a, out var neighbors) && neighbors.Contains(b);
    }
}