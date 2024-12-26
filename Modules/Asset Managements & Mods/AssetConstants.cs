namespace Cutulu
{
    public static class AssetConstants
    {
        public readonly static char[] NAME_SEPERATOR = new[] { '/', '\\' };
        public const IO.FileType FILE_TYPE = IO.FileType.Json;
        public const string ADDRESS_SEPERATOR = "::";
        public const string FILE_ENDING = ".cm";

        public static readonly string[] ClassicModPaths = new[] {
            $"{IO.PROJECT_PATH}Mods/", $"{IO.PROJECT_PATH}Assets/", $"{IO.PROJECT_PATH}Patches/",
            $"{IO.USER_PATH}Mods/", $"{IO.USER_PATH}Assets/", $"{IO.USER_PATH}Patches/"
        };
    }
}