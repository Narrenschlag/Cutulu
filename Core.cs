using System.Collections.Generic;
using System.Text;
using System;

using Godot;

namespace Cutulu
{
	public static class Core
	{
		#region Shortcuts
		public static Node GetRoot(this Node node) => node.GetTree().CurrentScene;

		public static Node3D Main3D { get { if (_main3d == null) _main3d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node3D>(); return _main3d; } }
		private static Node3D _main3d;

		public static Node2D Main2D { get { if (_main2d == null) _main2d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node2D>(); return _main2d; } }
		private static Node2D _main2d;

		public static Node Main { get { if (_main == null) _main = (Engine.GetMainLoop() as SceneTree).CurrentScene; return _main; } }
		private static Node _main;
		#endregion

		#region General Functions
		public static float invert(this float value, bool invert) => value * (invert ? -1 : 1);
		public static int invert(this int value, bool invert) => value * (invert ? -1 : 1);

		public static int toInt(this bool value, int _true, int _false) => value ? _true : _false;
		public static int toInt01(this bool value) => value ? 1 : 0;
		#endregion

		#region Rotation Functions
		public static Vector2 RotatedD(this Vector2 v2, float degrees) => v2.Rotated(degrees.toRadians());

		public static float toDegrees(this float radians) => radians / Mathf.Pi * 180;
		public static float toRadians(this float degree) => degree / 180 * Mathf.Pi;

		public static void lerpLookY(this Node3D node, Vector3 globalPosition, float lerp)
		{
			Vector3 Rotation = node.Rotation;
			node.LookAt(globalPosition);

			node.Rotation = Rotation.setY(Mathf.LerpAngle(Rotation.Y, node.Rotation.Y, lerp));
		}

		public static void lerpLookYDir(this Node3D node, Vector3 dir, float lerp, bool global = false)
		{
			float angle = Vector2.Zero.AngleToPoint(dir.toXY().RotatedD(90));

			if (global) node.GlobalRotation = node.GlobalRotation.setY(Mathf.LerpAngle(node.GlobalRotation.Y, angle, lerp));
			else node.Rotation = node.Rotation.setY(Mathf.LerpAngle(node.Rotation.Y, angle, lerp));
		}
		#endregion

		#region Node Functions
		public static void Clear(this Node parent, bool forceInstant = false)
		{
			if (parent.IsNull()) return;

			foreach (Node child in parent.GetChildren())
			{
				if (child.IsNull()) continue;

				child.Destroy(forceInstant);
			}
		}

		public static void DestroyAfter(this Node node, float lifeTime, bool forceInstant = false) => QueueAction(lifeTime, delegate { node.Destroy(forceInstant); });
		public static void Destroy(this Node node, bool forceInstant = false)
		{
			if (node.IsNull()) return;

			if (forceInstant) node.Free();
			else node.QueueFree();
		}

		public static Vector3 Forward(this Node3D node, bool global = true) => node == null ? Vector3.Forward : (global ? node.GlobalTransform : node.Transform).Basis.Z;
		public static Vector3 Right(this Node3D node, bool global = true) => node == null ? Vector3.Right : (global ? node.GlobalTransform : node.Transform).Basis.X;
		public static Vector3 Up(this Node3D node, bool global = true) => node == null ? Vector3.Up : (global ? node.GlobalTransform : node.Transform).Basis.Y;

		public static T Instantiate<T>(this PackedScene prefab, Node root) where T : Node
		{
			if (prefab == null) return null;

			T t = (T)prefab.Instantiate();
			root.AddChild(t);

			return t;
		}

		public static void SetActive(this Node node, bool active, bool includeChildren = false)
		{
			node.ProcessMode = active ? Node.ProcessModeEnum.Pausable : Node.ProcessModeEnum.Disabled;
			if (node is CollisionObject3D) (node as CollisionObject3D).DisableMode = active ? CollisionObject3D.DisableModeEnum.KeepActive : CollisionObject3D.DisableModeEnum.Remove;
			if (node is CollisionShape3D) (node as CollisionShape3D).Disabled = !active;
			if (node is CanvasItem) (node as CanvasItem).Visible = active;
			if (node is Node3D) (node as Node3D).Visible = active;
			if (includeChildren)
			{
				foreach (Node child in node.GetNodesInChildren<Node>(false))
					SetActive(child, false);
			}
		}

		public static List<T> GetNodesInChildren<T>(this Node node, bool includeSelf = true) where T : Node
		{
			List<T> list = new List<T>();
			loop(node, includeSelf);
			return list;

			void loop(Node node, bool includeSelf)
			{
				if (includeSelf) add(node);

				foreach (Node n in node.GetChildren())
				{
					loop(n, true);
				}
			}

			void add(Node node)
			{
				if (node is T) list.Add(node as T);
			}
		}

		public static T GetNodeInChildren<T>(this Node node, bool includeSelf = true) where T : Node
		{
			T result = null;
			loop(node, includeSelf);
			return result;

			void loop(Node node, bool includeSelf)
			{
				if (result != null) return;

				if (includeSelf && set(node))
					return;

				foreach (Node n in node.GetChildren())
				{
					loop(n, true);
				}
			}

			bool set(Node node)
			{
				if (!(node is T)) return false;

				result = node as T;
				return true;
			}
		}

		public static T GetNode<T>(this Node node) where T : Node => node is T ? node as T : null;
		public static bool TryGetNode<T>(this Node node, out T result) where T : Node
		{
			if (node is T)
			{
				result = node as T;
				return true;
			}

			result = null;
			return false;
		}

		public static bool IsNull(this GodotObject node) => node == null || !GodotObject.IsInstanceValid(node);
		public static bool NotNull(this GodotObject node) => !IsNull(node);
		#endregion

		#region Signal Functions
		public static void Connect(this Node signalSource, string signalName, Node nodeToConnect, string functionName)
		{
			if (signalSource.IsNull() || signalName.IsEmpty() || nodeToConnect.IsNull() || functionName.IsEmpty()) return;
			signalSource.Connect(signalName, new Callable(nodeToConnect, functionName));
		}

		public static void Disconnect(this Node signalSource, string signalName, Node nodeToConnect, string functionName)
		{
			if (signalSource.IsNull() || signalName.IsEmpty() || nodeToConnect.IsNull() || functionName.IsEmpty()) return;
			signalSource.Disconnect(signalName, new Callable(nodeToConnect, functionName));
		}

		public static void ConnectButton(this Node node, Node nodeToConnect, string functionName) => Connect(node, "pressed", nodeToConnect, functionName);
		public static void DisconnectButton(this Node node, Node nodeToConnect, string functionName) => Disconnect(node, "pressed", nodeToConnect, functionName);
		#endregion

		#region Bit Functions
		public static float GetBitAt_float(this byte number, int bitIndex) => GetBitAt((int)number, bitIndex) ? 1 : 0;
		public static bool GetBitAt(this byte number, int bitIndex) => GetBitAt((int)number, bitIndex);
		public static bool GetBitAt(this int number, int bitIndex)
		{
			if (bitIndex < 0 || bitIndex >= 32) // C# Integer have 32 bits
				throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");

			return ((number >> bitIndex) & 1) == 1;
		}

		public static byte SetBitAt(ref byte number, int bitIndex, bool newValue) => number = SetBitAt(number, bitIndex, newValue);
		public static byte SetBitAt(this byte number, int bitIndex, bool newValue) => (byte)SetBitAt((int)number, bitIndex, newValue);

		public static int SetBitAt(ref int number, int bitIndex, bool newValue) => number = SetBitAt(number, bitIndex, newValue);
		public static int SetBitAt(this int number, int bitIndex, bool newValue)
		{
			if (bitIndex < 0 || bitIndex >= 32) // C# Integer have 32 bits
				throw new ArgumentOutOfRangeException(nameof(bitIndex), "Bit index is out of range.");

			// Set bit to 0
			number &= ~(1 << bitIndex);

			// If newValue is true, set bit to 1
			if (newValue) number |= (1 << bitIndex);

			return number;
		}

		public static bool[] GetBitsArray(this int number)
		{
			int numBits = 32; // Annahme: Ein int hat 32 Bits in C#
			bool[] bits = new bool[numBits];

			for (int i = 0; i < numBits; i++)
			{
				bits[i] = ((number >> i) & 1) == 1;
			}

			System.Array.Reverse(bits); // Umkehrung, um die richtige Reihenfolge zu erhalten
			return bits;
		}
		#endregion

		#region Array Functions
		public static bool NotEmpty<T>(this T[] array) => array != null && array.Length > 0;
		public static bool IsEmpty<T>(this T[] array) => !NotEmpty(array);

		public static T RandomElement<T>(this T[] array, T @default = default(T)) => array.NotEmpty() ? array[Random.RangeInt(0, array.Length)] : @default;

		public static T GetClampedElement<T>(this T[] array, int index) => array.IsEmpty() ? default(T) : array[Mathf.Clamp(index, 0, array.Length - 1)];
		#endregion

		#region List Functions
		public static bool NotEmpty<T>(this List<T> list) => list != null && list.Count > 0;
		public static bool IsEmpty<T>(this List<T> list) => !NotEmpty(list);

		public static T RandomElement<T>(this List<T> list, T @default = default(T)) => list.NotEmpty() ? list[Random.RangeInt(0, list.Count)] : @default;
		#endregion

		#region Dictionary Functions
		public static bool NotEmpty<T, U>(this Dictionary<T, U> dic) => dic != null && dic.Count > 0;
		public static bool IsEmpty<T, U>(this Dictionary<T, U> dic) => !NotEmpty(dic);
		#endregion

		#region Float Functions
		public static float abs(this float f) => Math.Abs(f);

		public static float max(this float f0, float f1) => Math.Max(f0, f1);
		public static float max(this float f0, float f1, float f2) => max(max(f0, f1), f2);
		public static float max(this float f0, float f1, float f2, float f3) => max(max(f0, f1, f2), f3);


		public static float min(this float f0, float f1) => Math.Min(f0, f1);
		public static float min(this float f0, float f1, float f2) => min(min(f0, f1), f2);
		public static float min(this float f0, float f1, float f2, float f3) => min(min(f0, f1, f2), f3);
		#endregion

		#region Integer Functions
		public static int abs(this int i) => Math.Abs(i);

		public static int max(this int f0, int f1) => Math.Max(f0, f1);
		public static int max(this int f0, int f1, int f2) => max(max(f0, f1), f2);
		public static int max(this int f0, int f1, int f2, int f3) => max(max(f0, f1, f2), f3);

		public static int min(this int f0, int f1) => Math.Min(f0, f1);
		public static int min(this int f0, int f1, int f2) => min(min(f0, f1), f2);
		public static int min(this int f0, int f1, int f2, int f3) => min(min(f0, f1, f2), f3);
		#endregion

		#region Vector2 Functions
		public static Vector2 Lerp(this Vector2 a, Vector2 b, float lerp) => new Vector2(Mathf.Lerp(a.X, b.X, lerp), Mathf.Lerp(a.Y, b.Y, lerp));

		public static Vector2 setX(this Vector2 v2, float value) => new Vector2(value, v2.Y);
		public static Vector2 setY(this Vector2 v2, float value) => new Vector2(v2.X, value);


		public static void pasteX(this float value, ref Vector2 v2) => v2.X = value;
		public static void pasteY(this float value, ref Vector2 v2) => v2.Y = value;

		public static Vector2I RoundToInt(this Vector2 v2) => new Vector2I(Mathf.RoundToInt(v2.X), Mathf.RoundToInt(v2.Y));
		public static Vector2I FloorToInt(this Vector2 v2) => new Vector2I(Mathf.FloorToInt(v2.X), Mathf.FloorToInt(v2.Y));
		public static Vector2I CeilToInt(this Vector2 v2) => new Vector2I(Mathf.CeilToInt(v2.X), Mathf.CeilToInt(v2.Y));
		public static Vector2 Abs(this Vector2 v2) => new Vector2(Mathf.Abs(v2.X), Mathf.Abs(v2.Y));
		public static Vector2 Max(this Vector2 o, Vector2 a, Vector2 b) => o.DistanceTo(a) > o.DistanceTo(b) ? a : b;
		public static Vector2 Min(this Vector2 o, Vector2 a, Vector2 b) => o.DistanceTo(a) < o.DistanceTo(b) ? a : b;
		public static Vector2 NoNaN(this Vector2 v2) => new Vector2(float.IsNaN(v2.X) ? 0 : v2.X, float.IsNaN(v2.Y) ? 0 : v2.Y);

		public static Vector2 toXY(this Vector3 value) => new Vector2(value.X, value.Z);
		#endregion

		#region Vector3 Functions
		public static void SetForward(this Node3D node, Vector3 direction, bool global = true) => node.LookAt((global ? node.GlobalPosition : node.Position) + direction);

		public static Vector3 setX(this Vector3 v3, float value) => new Vector3(value, v3.Y, v3.Z);
		public static Vector3 setY(this Vector3 v3, float value) => new Vector3(v3.X, value, v3.Z);
		public static Vector3 setZ(this Vector3 v3, float value) => new Vector3(v3.X, v3.Y, value);
		public static Vector3 multX(this Vector3 v3, float value) => new Vector3(v3.X * value, v3.Y, v3.Z);
		public static Vector3 multY(this Vector3 v3, float value) => new Vector3(v3.X, v3.Y * value, v3.Z);
		public static Vector3 multZ(this Vector3 v3, float value) => new Vector3(v3.X, v3.Y, v3.Z * value);

		public static void pasteX(this float value, ref Vector3 v3) => v3.X = value;
		public static void pasteY(this float value, ref Vector3 v3) => v3.Y = value;
		public static void pasteZ(this float value, ref Vector3 v3) => v3.Z = value;

		public static Vector3 toRight(this Vector3 forward) => toRight(forward, Vector3.Up);
		public static Vector3 toRight(this Vector3 forward, Vector3 up) => forward.Normalized().Cross(up.Normalized());

		public static Vector3 toUp(this Vector3 forward) => toRight(forward, Vector3.Right);
		public static Vector3 toUp(this Vector3 forward, Vector3 right) => -forward.Normalized().Cross(right.Normalized());

		public static Vector3 toXZ(this Vector2 value) => new Vector3(value.X, 0, value.Y);
		#endregion

		#region List Functions
		public static List<Vector3> ClampDistanceRelative(this List<Vector3> list, float percentage)
		{
			percentage = Mathf.Clamp(percentage, 0, 1);
			if (percentage >= 1 || list.IsEmpty()) return list;
			if (percentage <= 0) return null;

			float sum = 0;

			for (int i = 0; i < list.Count - 1; i++)
				sum += list[i].DistanceTo(list[i + 1]);

			return ClampDistance(list, sum * percentage);
		}

		public static List<Vector2> ClampDistanceRelative(this List<Vector2> list, float percentage)
		{
			percentage = Mathf.Clamp(percentage, 0, 1);
			if (percentage >= 1 || list.IsEmpty()) return list;
			if (percentage <= 0) return null;

			float sum = 0;

			for (int i = 0; i < list.Count - 1; i++)
				sum += list[i].DistanceTo(list[i + 1]);

			return ClampDistance(list, sum * percentage);
		}

		public static List<Vector3> ClampDistance(this List<Vector3> list, float distance)
		{
			List<Vector3> result = new List<Vector3>();
			float current = 0;

			for (int i = 0; i < list.Count - 1; i++)
			{
				float next = current + list[i].DistanceTo(list[i + 1]);

				// Overtook distance
				if (next > distance)
				{
					float a = next - distance;
					float b = next - current;

					result.Add(list[i].Lerp(list[i + 1], (b - a) / b));
					break;
				}

				result.Add(list[i]);

				// Stopped exactly there
				if (next == distance) break;

				current = next;
			}

			return result;
		}

		public static List<Vector2> ClampDistance(this List<Vector2> list, float distance)
		{
			List<Vector2> result = new List<Vector2>();
			float current = 0;

			for (int i = 0; i < list.Count - 1; i++)
			{
				float next = current + list[i].DistanceTo(list[i + 1]);

				// Overtook distance
				if (next > distance)
				{
					float a = next - distance;
					float b = next - current;

					result.Add(list[i].Lerp(list[i + 1], (b - a) / b));
					break;
				}

				result.Add(list[i]);

				// Stopped exactly there
				if (next == distance) break;

				current = next;
			}

			return result;
		}
		#endregion

		#region Input Functions
		public static float GetInputValue(this string name) => Input.GetActionRawStrength(name);

		public static Vector2 GetInputVector(this string negativeX, string positiveX, string negativeY, string positiveY) => new Vector2(GetInputAxis(negativeX, positiveX), GetInputAxis(negativeY, positiveY));
		public static float GetInputAxis(this string negative, string positive) => -negative.GetInputValue() + positive.GetInputValue();
		public static bool GetInput(this string name, float threshold = .2f) => GetInputValue(name) >= threshold;
		#endregion

		#region String Functions
		public static string TrimToDirectory(this string path)
		{
			if (string.IsNullOrEmpty(path) || !(path.Contains('/') || path.Contains('\\')))
				return path;

			char c;
			for (int i = path.Length; i > 0; i--)
			{
				c = path[i - 1];

				if (c.Equals('\\') || c.Equals('/'))
				{
					return path.Substring(0, i - 1);
				}
			}

			return path;
		}

		public static string ReplaceFirst(this string str, char c, object value)
		{
			for (int i = 0; i < str.Length; i++)
			{
				if (!str[i].Equals(c)) continue;

				str = str.Substring(0, i++) + value + str.Substring(i, str.Length - i);
				break;
			}

			return str;
		}

		public static string Fill(this string str, int targetLength, char fillChar, bool before = true)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(str.Trim());

			for (int i = 0; i < (targetLength - builder.Length); i++)
				if (before) builder.Insert(0, fillChar);
				else builder.Append(fillChar);

			return builder.ToString();
		}

		public static bool IsEmpty(this string str) => string.IsNullOrEmpty(str);
		public static bool NotEmpty(this string str) => !IsEmpty(str);

		// Kind of splits up a string to only write down the contents between the signals. Nice for a lot of stuff.
		public static string Extract(this string source, char signal, out string[] extracted, bool removeSignal = true) => Extract(source, new List<char>() { signal }, out extracted, removeSignal);
		public static string Extract(this string source, List<char> signals, out string[] extracted, bool removeSignals = true)
		{
			extracted = null;

			if (source.IsEmpty()) return source;

			StringBuilder result = new StringBuilder();

			List<string> _extracted = new List<string>();
			StringBuilder str = new StringBuilder();
			bool active = false;

			for (int c = 0; c < source.Length; c++)
			{
				if (signals.Contains(source[c]))
				{
					active = !active;

					if (!active && !removeSignals) str.Append(source[c]);

					if (str.ToString().NotEmpty())
						_extracted.Add(str.ToString());
					str.Clear();

					if (active && !removeSignals) str.Append(source[c]);
				}
				else
				{
					if (active) str.Append(source[c]);
					else result.Append(source[c]);
				}
			}

			if (str.ToString().NotEmpty()) _extracted.Add(str.ToString());

			extracted = _extracted.ToArray();
			return result.ToString();
		}

		public static bool IsEmail(this string mail)
		{
			if (mail.IsEmpty()) return false;
			if (!mail.Contains('@')) return false;
			if (mail.EndsWith('.') || mail.EndsWith('@')) return false;

			string[] splits = mail.Split('@');
			if (splits.Length != 2) return false;
			if (!splits[1].Contains('.')) return false;

			return true;
		}
		#endregion

		#region Enum Functions
		public static Array Array(this Enum Enum) => Enum.GetValues(Enum.GetType());
		public static int Length(this Enum Enum) => Array(Enum).Length;

		public static Array EnumArray<T>() where T : Enum => Enum.GetValues(typeof(T));
		public static int EnumLength<T>() where T : Enum => EnumArray<T>().Length;
		#endregion

		#region Wait
		private static WaiterHandler wh;
		/// <summary>
		/// Has to be setup with a master parent if there is none.<br/>
		/// <br/><b>seconds</b>     The duration to wait in seconds
		/// <br/><b>action</b>      The callback function
		/// <br/><b>root parent</b> The parent node (has to be referenced once to be set up
		/// </summary>
		public static void QueueAction(this float seconds, Action action, Node rootParent = null)
		{
			if (action == null) return;

			// Create Wait Handler if not existant yet
			if (wh == null)
			{
				if (rootParent == null)
				{
					Debug.LogError("NoParentNodeException: Cannot add any wait timers until there is a node in the tree!");
					return;
				}

				wh = new WaiterHandler();
				rootParent.AddChild(wh);
			}

			// Add action to queue
			if (seconds <= 0)
			{
				if (action != null) action();
			}
			else wh.Add(action, seconds);
		}
		#endregion
	}

	#region Wait Extension
	public partial class WaiterHandler : Node
	{
		public List<WaitObj> Objects = new List<WaitObj>();

		public void Add(Action action, float time) => Objects.Add(new WaitObj() { Action = action, TimeRemaining = time });

		public override void _Process(double delta)
		{
			if (Objects.IsEmpty()) return;
			float time = (float)delta;

			for (int i = Objects.Count - 1; i >= 0; i--)
			{
				Objects[i].TimeRemaining -= time;

				if (Objects[i].TimeRemaining <= 0)
				{
					Objects[i].Action.Invoke();
					Objects.RemoveAt(i);
				}
			}
		}

		public class WaitObj
		{
			public float TimeRemaining;
			public Action Action;
		}
	}
	#endregion
}