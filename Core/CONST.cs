namespace Cutulu.Core
{
    using System;

    public static class CONST
    {
        public const StringSplitOptions StringSplit = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        public const float GoldenCut = 1.618f;

        public const string LocalHostIPv4 = "127.0.0.1";
        public const string LocalHostIPv6 = "::1";

        public const string PROJECT_PATH = "res://";
        public const string USER_PATH = "user://";
    }
}