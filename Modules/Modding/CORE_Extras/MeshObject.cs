using Godot;

namespace Cutulu.Modding
{
    [GlobalClass]
    public partial class MeshObject : Resource
    {
        [Export] public Vector3 Position { get; set; }
        [Export] public Vector3 Rotation { get; set; }
        [Export] public Vector3 Scale { get; set; } = Vector3.One;

        [Export] public string MeshGLB { get; set; }
        [Export] public string BaseMaterial { get; set; }
        [Export] public string[] Materials { get; set; }

        public Node3D Instantiate(CORE core, Node parent)
        {
            var model = core.GetResource<GlbModel>(MeshGLB);
            if (model.IsNull()) return null;

            var meshInstance = model.Instantiate<Node3D>(parent);
            meshInstance.RotationDegrees = Rotation;
            meshInstance.Position = Position;
            meshInstance.Scale = Scale;

            var baseMaterial = core.GetResource<StandardMaterial3D>(BaseMaterial);
            var meshes = meshInstance.GetNodesInChildren<MeshInstance3D>();
            var m = Materials.NotEmpty() ? 0 : int.MinValue;

            if ((baseMaterial != null || m >= 0) && meshes.NotEmpty())
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    var count = meshes[i].GetSurfaceOverrideMaterialCount();

                    for (int k = 0; k < count; k++, m++)
                    {
                        if (m >= 0 && m < Materials.Length)
                        {
                            var material = core.GetResource<StandardMaterial3D>(Materials[m]);

                            meshes[i].SetSurfaceOverrideMaterial(k, material);
                        }

                        else
                        {
                            meshes[i].SetSurfaceOverrideMaterial(k, baseMaterial);
                        }
                    }
                }
            }

            return meshInstance;
        }
    }
}