using System.Collections.Generic;
using Godot;

namespace Cutulu.Core
{
	public partial class MaterialOverrider : Node3D
	{
		[Export] protected string MetaKey = "id_";
		[Export] protected MeshInstance3D[] AffectedNodes;
		[Export] private Material[] Materials;

		public string Meta(int i) => $"{MetaKey}{i}";

		public virtual void Override(Material[] materials = null)
		{
			if (materials.IsEmpty()) materials = Materials;

			if (string.IsNullOrEmpty(MetaKey)) return;
			if (AffectedNodes.IsEmpty()) return;
			if (materials.IsEmpty()) return;

			foreach (MeshInstance3D mesh in AffectedNodes)
				Override(mesh, materials);
		}

		protected virtual void Override(MeshInstance3D mesh, Material[] materials)
		{
			if (mesh == null || mesh.Mesh == null) return;

			foreach (KeyValuePair<int, int> pair in GetMetas<int>(mesh))
				mesh.SetSurfaceOverrideMaterial(pair.Key, materials[pair.Value]);
		}

		protected virtual List<KeyValuePair<int, T>> GetMetas<[MustBeVariant] T>(MeshInstance3D mesh, T @default = default(T))
		{
			List<KeyValuePair<int, T>> metas = new List<KeyValuePair<int, T>>();

			for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
				if (mesh.HasMeta(Meta(i)))
					metas.Add(new KeyValuePair<int, T>(i, mesh.GetMeta(Meta(i), 0).As<T>()));

			return metas;
		}
	}
}
