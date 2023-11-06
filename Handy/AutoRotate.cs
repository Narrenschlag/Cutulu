using Godot;

namespace Cutulu
{
	public partial class AutoRotate : Node3D
	{
		[Export] private Vector3 Speed;

		protected virtual Vector3 RotationSpeed => Speed;

		public override void _Process(double delta)
		{
			RotationDegrees += (float)delta * Speed;
		}
	}
}
