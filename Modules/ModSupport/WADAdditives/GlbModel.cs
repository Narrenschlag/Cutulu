using Godot;

namespace Cutulu
{
    public partial class GlbModel : GltfDocument
    {
        public GltfDocument Data { get; private set; }
        public GltfState State { get; private set; }

        public static GlbModel CustomImport(byte[] bytes)
        {
            if (bytes.NotEmpty())
            {
                var model = new GlbModel()
                {
                    State = new(),
                    Data = new(),
                };

                model.Data.AppendFromBuffer(bytes, "", model.State, 8);
                return model;
            }

            else return default;
        }

        public Node GenerateScene() => GenerateScene<Node>();
        public Node3D GenerateScene3D() => GenerateScene<Node3D>();
        public T GenerateScene<T>() where T : Node => (T)Data.GenerateScene(State);
    }
}