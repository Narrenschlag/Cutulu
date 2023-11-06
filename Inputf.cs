using Godot;

namespace Cutulu
{
	public static class Inputf
	{
		public static bool GetKey(this string name, float threshhold = .5f) => GetValue(name) >= threshhold;
		public static float GetValue(this string name) => Input.GetActionRawStrength(name);

		public static Vector2 MousePosition(this Node node) => node.GetViewport().GetMousePosition();

		public static bool Down(this string name, ref bool valueStore, float threshold = .5f)
		{
			bool old = valueStore;

			valueStore = name.GetKey(threshold);
			return !old && valueStore;
		}

		public static bool Up(this string name, ref bool valueStore, float threshold = .5f)
		{
			bool old = valueStore;

			valueStore = name.GetKey(threshold);
			return old && !valueStore;
		}
	}
}
