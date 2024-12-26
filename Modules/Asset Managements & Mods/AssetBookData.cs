namespace Cutulu
{
    public partial class AssetBookData
    {
        public string Id { get; set; }
        public int DefaultPriority { get; set; }
        public string[] Dependencies { get; set; }
        public bool ForceEnabled { get; set; }

        public string Author { get; set; }
        public string Version { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string[] DllPaths { get; set; }
        public string[] PckPaths { get; set; }

        public string[] AliasIndex { get; set; }
    }
}