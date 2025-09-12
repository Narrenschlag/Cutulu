namespace Cutulu.Core.Logging;

using System;

public class Logger
{
    public readonly DateTime Time = DateTime.Now;
    public readonly uint LogCount;
    public readonly string Path;

    public readonly System.Text.StringBuilder Text = new();
    public string LogUID { get; set; } = "";
    private uint LogIdx { get; set; } = 0;
    public uint Id { get; set; } = 0;

    private string TimeString { get; set; } = null;

    public Logger(string path = $"user://logs$uid/$time/log_$id.txt", uint logCount = 65536 /*1024 x 64*/, string logUid = "")
    {
        LogCount = logCount;
        LogUID = logUid;
        Path = path;
    }

    public virtual string GetPath()
    {
        if (TimeString.IsEmpty()) TimeString = $"D-{Time.Year}-{Time.Month}-{Time.Day}-T{Time.Hour:D2}{Time.Minute:D2}{Time.Second:D2}";

        return Path
        .Replace("$uid", LogUID.IsEmpty() ? "" : $"_{LogUID}")
        .Replace("$id", Id.ToString())
        .Replace("$time", TimeString);
    }

    public void Write()
    {
        var text = Text.ToString();
        Text.Clear();
        LogIdx = 0;
        Id++;

        if (text.IsNull() || text.Length < 1) return;

        var file = new File(GetPath());
        file.WriteString(text);

#if GODOT4_0_OR_GREATER
        // Print via Godot to prevent it appearing in the logs
        Godot.GD.PrintRich($"[color=magenta][b][{GetType().Name}][/b][/color] Wrote log file to {file.SystemPath}: [b][pulse]{file.Exists()}");
#endif
    }

    public void Log(string line)
    {
        Text.AppendLine(line);

        if (++LogIdx >= LogCount) Write();
    }
}