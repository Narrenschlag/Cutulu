namespace Cutulu.Core;

using System.Diagnostics;
using System;

public partial class Executable : File
{
    #region Constructor

    public Executable(string path) : base(path)
    {
        if (SystemPath.IsExecutablePath() == false)
            throw new ArgumentException($"Executable file extension '{SystemPath}' is not supported. Please assign a valid '{Application.OSExecFileExtension}' executable file path.");
    }

    #endregion

    public Process Execute(string args = "", bool shell = false, bool newWindow = true)
    {
        if (this.Exists() == false) throw new System.IO.FileNotFoundException($"Executable '{SystemPath}' not found.");

        // Example: launch "myapp" with arguments
        return StartProcess(new ProcessStartInfo
        {
            FileName = SystemPath,
            Arguments = args,
            UseShellExecute = shell,
            CreateNoWindow = newWindow == false,
        });
    }

    public Process ExecuteGodot(string args = "", bool fullscreen = false)
    {
        if (this.Exists() == false) throw new System.IO.FileNotFoundException($"Executable '{SystemPath}' not found.");

        args ??= "";

        if (fullscreen) args += " --fullscreen";

        // Example: launch "myapp" with arguments
        return StartProcess(new ProcessStartInfo
        {
            FileName = SystemPath,
            Arguments = args,
        });
    }

    private static Process StartProcess(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo);
    }
}
