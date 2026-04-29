namespace Cutulu.Patching;

using System.Collections.Generic;
using Core;

public class Manifest
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public int ChunkSize { get; set; } = Builder.DefaultChunkSize;
    public Dictionary<string, List<string>> Files { get; set; } = [];
}