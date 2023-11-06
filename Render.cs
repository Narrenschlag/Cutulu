using System.Collections.Generic;
using Godot;

namespace Cutulu
{
	public static class Render
	{
		#region Base Functions
		public static MeshInstance3D StartLineMesh(this Color color, out ImmediateMesh mesh)
		{
			MeshInstance3D mesh_instance = new MeshInstance3D();
			OrmMaterial3D material = new OrmMaterial3D();
			mesh = new ImmediateMesh();

			mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh_instance.Mesh = mesh;

			material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
			material.AlbedoColor = color;

			mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
			return mesh_instance;
		}

		public static void EndLineMesh(this ImmediateMesh mesh)
		{
			mesh.SurfaceEnd();
		}

		public static MeshInstance3D DrawLine(this Node node, List<Vector3> points, Color color)
		{
			if (color.A <= 0 || points.IsEmpty())
			{
				Debug.LogError($"CannotDrawLineError: No points have been given");
				return null;
			}

			if (points.Count < 2) return DrawPoint(node, points[0], color);

			MeshInstance3D mesh_instance = StartLineMesh(color, out ImmediateMesh mesh);
			for (int i = 1; i < points.Count; i++)
			{
				mesh.SurfaceAddVertex(points[i - 1]);
				mesh.SurfaceAddVertex(points[i]);
			}

			mesh.EndLineMesh();

			node.AddChild(mesh_instance);
			return mesh_instance;
		}

		public static MeshInstance3D DrawPoint(this Node node, Vector3 position, Color color, float radius = .05f)
		{
			MeshInstance3D mesh_instance = new MeshInstance3D();
			OrmMaterial3D material = new OrmMaterial3D();
			SphereMesh sphere_mesh = new SphereMesh();

			mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			mesh_instance.Position = position;
			mesh_instance.Mesh = sphere_mesh;

			sphere_mesh.Material = material;
			sphere_mesh.Height = radius * 2;
			sphere_mesh.Radius = radius;

			material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
			material.AlbedoColor = color;

			node.AddChild(mesh_instance);
			return mesh_instance;
		}
		#endregion

		#region Line Functions
		public static MeshInstance3D DrawRay(this Node node, Vector3 source, Vector3 direction, Color color) => DrawLine(node, new List<Vector3>() { source, source + direction }, color);
		public static MeshInstance3D DrawLine(this Node node, Vector3 from, Vector3 to, Color color) => DrawLine(node, new List<Vector3>() { from, to }, color);
		#endregion

		#region Curve Functions
		public static MeshInstance3D DrawCurve(Vector3 origin, Vector3 direction, Color color, float gravity, float resolution, float length)
			=> DrawCurve(Core.Main3D, origin, direction, color, gravity, resolution, length);

		public static MeshInstance3D DrawCurve(this Node3D node, Color color, float gravity, float resolution, float length)
			=> DrawCurve(node, node.GlobalPosition, node.Forward(), color, gravity, resolution, length);

		public static MeshInstance3D DrawCurve(this Node node, Vector3 origin, Vector3 direction, Color color, float gravity, float resolution, float length)
		{
			if (length <= 0) return null;

			int count = Mathf.FloorToInt(length / resolution);
			float rest = length - count * resolution;
			if (count < 1 && rest <= 0) return null;

			MeshInstance3D mesh_instance = StartLineMesh(color, out ImmediateMesh mesh);
			Vector3 right = direction.toRight();
			Vector3 last = origin;
			gravity *= resolution;

			// Draw main part
			for (int i = 0; i < count; i++) add();

			// Draw rest
			if (rest > 0) add(rest / resolution);

			// Return result
			mesh.EndLineMesh();
			node.AddChild(mesh_instance);
			return mesh_instance;

			void rotate() => direction = direction.Rotated(right, gravity);
			void add(float value = 1f)
			{
				mesh.SurfaceAddVertex(last);

				rotate();
				last += direction * value * resolution;
				mesh.SurfaceAddVertex(last);
			}
		}
		#endregion
	}
}
