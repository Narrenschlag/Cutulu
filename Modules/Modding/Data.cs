namespace Cutulu.Modding
{
    public partial struct Data
    {
        public string Id { get; set; }
        public int DefaultPriority { get; set; }

        public string Author { get; set; }
        public string Version { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string[] Dependencies { get; set; }
        public string[] LocalAddresses { get; set; }
    }
}