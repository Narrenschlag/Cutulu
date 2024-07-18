using System.Text;
using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// Contains important meta data as author, name, icon and description. Aswell as the file index used to locate the files that can be loaded.
    /// </summary>
    public class COREMeta
    {
        #region Params
        public const string META_PATH = $"core{META_ENDING}";
        public const string META_ENDING = ".index";

        public string COREId { get; set; } = "generic_core_id";
        public string Author { get; set; } = "Narrenschlag";
        public string Version { get; set; } = "1.0.0.0";

        public string Name { get; set; } = "Core Asset Pack";
        public string Description { get; set; } = "Utilizing CORE for mod support and asset delivery.";

        public string[] Dependencies { get; set; } = null;
        public string[] Index { get; set; } = null;

        public int GetFileCount() => Index.Size();
        #endregion

        #region Constructors
        public COREMeta() { }

        /// <summary>
        /// Creates custom CORE meta. Has to be written down by using CORE.Compile.
        /// </summary>
        public COREMeta(string author, string name, string description, params string[] index)
        {
            Author = author;

            Name = name;
            Description = description;

            Index = index;
        }
        #endregion

        public byte[] GetBuffer() => Encoding.UTF8.GetBytes(this.json());

        #region Read Data
        /// <summary>
        /// Tries to read meta file from CORE. Returns true if found and readable.
        /// </summary>
        public static bool TryRead(string filePath, out COREMeta meta)
        {
            return OE.TryGetData(filePath, out meta, IO.FileType.Json);
        }
        #endregion

        #region Debug
        public string GetMessage()
        {
            return $"### CORE META - {Name}({Index.Size()} files)\nby {Author}\n{Description}";
        }
        #endregion
    }
}