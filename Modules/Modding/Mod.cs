using System.Text;
using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// Contains important meta data as author, name, icon and description. Aswell as the file index used to locate the files that can be loaded.
    /// </summary>
    public class Mod
    {
        #region Params
        public const string FILE_ENDING = ".mod";

        public string Author { get; set; } = "author_name";
        public string Version { get; set; } = "1.0.0.0";
        public string ModId { get; set; } = "mod_id";
        public string Name { get; set; } = "mod_name";

        public string Description { get; set; } = "mod_description";

        public string[] Dependencies { get; set; } = null;
        public string[] Index { get; set; } = null;

        public int GetFileCount() => Index.Size();
        #endregion

        #region Constructors
        public Mod() { }

        /// <summary>
        /// Creates custom mod. Has to be written down by using Manager.Compile.
        /// </summary>
        public Mod(string author, string name, string description, params string[] index)
        {
            Author = author;
            Index = index;
            Name = name;

            Description = description;
        }
        #endregion

        public byte[] GetBuffer() => Encoding.UTF8.GetBytes(this.json());

        #region Read Data
        /// <summary>
        /// Tries to read mod file from directory. Returns true if found and readable.
        /// </summary>
        public static bool TryRead(string filePath, out Mod meta)
        {
            return OE.TryGetData(filePath, out meta, IO.FileType.Json);
        }
        #endregion

        #region Debug
        public string GetMessage()
        {
            return $"### Mod - {Name}({Index.Size()} files)\nby {Author}\n{Description}";
        }
        #endregion
    }
}