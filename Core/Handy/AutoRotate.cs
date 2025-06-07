using Godot;

namespace Cutulu.Core
{
	public partial class AutoRotate : Node3D
	{
		[Export] public Vector3 Speed;

		public override void _Process(double delta)
		{
			RotationDegrees += (float)delta * Speed;
		}
	}
}
