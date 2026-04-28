namespace Cutulu.Patching;

using System.Collections.Generic;
using Core;

public class Manifest
{
    [Encodable] public Dictionary<string, List<string>> Files = [];
}