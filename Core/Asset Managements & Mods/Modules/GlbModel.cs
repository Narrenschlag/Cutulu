namespace Cutulu.Core
{
    using Godot;

    /// <summary> 
    /// Used as backend for MeshObject.cs keeping track of the glb model and its data.
    /// <summary>
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

        public T Instantiate<T>(Node parent = null) where T : Node
        {
            var node = GenerateScene<T>();

            if (parent.NotNull()) parent.AddChild(node);

            if (node is Node3D node3D)
            {
                node3D.Position = Vector3.Zero;
            }

            return node;
        }
    }
}