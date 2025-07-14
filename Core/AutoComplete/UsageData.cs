namespace Cutulu.Core.AutoComplete;

using System;

public class UsageData
{
    public string NormalizedName;
    public int UseCount = 0;
    public int SessionCount = 0;
    public DateTime LastUsed = DateTime.MinValue;
    public bool IsManuallyFavorited = false;

    public UsageData(string name) => NormalizedName = name;
}