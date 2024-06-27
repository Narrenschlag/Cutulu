using Godot;

namespace Cutulu.Modding
{
    /// <summary>
    /// Contains important meta data as author, name, icon and description. Aswell as the file index used to locate the files that can be loaded.
    /// </summary>
    public class COREMeta
    {
        #region Params
        public string COREId { get; set; } = "my_core";
        public string IconLocation { get; set; } = "icon.png";
        public string Author { get; set; } = "Narrenschlag";

        public string Name { get; set; } = "Asset Pack";
        public string Description { get; set; } = "Assets for everyone! Yippie!1!";

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

        #region Read Data
        /// <summary>
        /// Tries to read meta file from CORE. Returns true if found and readable.
        /// </summary>
        public static bool TryRead(string filePath, out COREMeta meta)
        {
            var reader = new ZipReader();

            var result = TryRead(ref reader, filePath, out meta);

            reader.Close();

            return result;
        }

        public static bool TryRead(ref ZipReader reader, string filePath, out COREMeta meta)
        {
            if (filePath.Exists())
            {
                if (reader.Open(filePath) == Error.Ok && reader.FileExists(CORE.META_PATH) && reader.ReadFile(CORE.META_PATH).TryBuffer(out meta))
                {
                    return meta != null;
                }
            }

            meta = default;
            return false;
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