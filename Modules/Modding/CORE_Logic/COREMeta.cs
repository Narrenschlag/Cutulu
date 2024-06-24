using Godot;

namespace Cutulu.Modding
{
    public class COREMeta
    {
        public string IconLocation { get; set; } = "icon.png";
        public string Author { get; set; } = "Narrenschlag";

        public string Name { get; set; } = "Asset Pack";
        public string Description { get; set; } = "Assets for everyone! Yippie!1!";

        public string[] Index { get; set; }

        public COREMeta() { }
        public COREMeta(string author, string name, string description, params string[] index)
        {
            Author = author;

            Name = name;
            Description = description;

            Index = index;
        }

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
                if (reader.Open(filePath) == Error.Ok && reader.FileExists("index.meta") && reader.ReadFile("index.meta").TryBuffer(out meta))
                {
                    return meta != null;
                }
            }

            meta = default;
            return false;
        }
    }
}