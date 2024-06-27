using Godot;

namespace Cutulu.Modding
{
    [GlobalClass]
    public partial class MeshObject : Resource
    {
        [Export] public Vector3 Position { get; set; }
        [Export] public Vector3 Rotation { get; set; }

        [Export] public string MeshGLB { get; set; }
        [Export] public string[] Materials { get; set; }

        public Node3D Instantiate(CORE core, Node parent)
        {
            var model = core.GetResource<GlbModel>(MeshGLB);
            if (model.IsNull()) return null;

            var meshInstance = model.Instantiate<Node3D>(parent);
            meshInstance.RotationDegrees = Rotation;
            meshInstance.Position = Position;

            if (Materials.NotEmpty())
            {
                var meshes = meshInstance.GetNodesInChildren<MeshInstance3D>();

                if (meshes.NotEmpty())
                {
                    var m = 0;

                    for (int i = 0; i < meshes.Count && m < Materials.Length; i++)
                    {
                        var count = meshes[i].GetSurfaceOverrideMaterialCount();

                        for (int k = 0; k < count && m < Materials.Length; k++, m++)
                        {
                            var material = core.GetResource<StandardMaterial3D>(Materials[m]);

                            meshes[i].SetSurfaceOverrideMaterial(k, material);
                        }
                    }
                }
            }

            return meshInstance;
        }
    }
}