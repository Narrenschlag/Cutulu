using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System;

using Godot;

namespace Cutulu
{
    public static class Core
    {
        #region Shortcuts               ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Node GetRoot(this Node node) => node.GetTree().CurrentScene;

        public static Node3D Main3D { get { if (_main3d == null) _main3d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node3D>(); return _main3d; } }
        private static Node3D _main3d;

        public static Node2D Main2D { get { if (_main2d == null) _main2d = (Engine.GetMainLoop() as SceneTree).CurrentScene.GetNodeInChildren<Node2D>(); return _main2d; } }
        private static Node2D _main2d;

        public static Node Main { get { if (_main == null) _main = (Engine.GetMainLoop() as SceneTree).CurrentScene; return _main; } }
        private static Node _main;
        #endregion

        #region General Functions       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float invert(this float value, bool invert) => value * (invert ? -1 : 1);
        public static int invert(this int value, bool invert) => value * (invert ? -1 : 1);

        public static int toInt(this bool value, int _true, int _false) => value ? _true : _false;
        public static int toInt01(this bool value) => value ? 1 : 0;
        #endregion

        #region Rotation Functions      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Vector2 RotatedD(this Vector2 v2, float degrees) => v2.Rotated(degrees.toRadians());

        public static float toDegrees(this float radians) => radians / Mathf.Pi * 180;
        public static float toRadians(this float degree) => degree / 180 * Mathf.Pi;

        public static float GetYRotation(this Vector3 direction, bool useRadians = false)
        {
            // Ensure the direction is normalized
            direction = direction.Normalized();

            // Calculate the angle using the arctangent function
            // Adjust the angle to be positive and between 0 and 360 units
            float angle = (Mathf.Atan2(direction.X, direction.Z) + Mathf.Pi * 2) % (Mathf.Pi * 2);

            // Convert the angle to degrees if needed
            return useRadians ? angle : angle.toDegrees();
        }

        public static Vector3 GetDirectionFromYRotation(this float angle, bool useRadians = false)
        {
            // Convert the angle to radians if needed
            if (useRadians)
            {
                angle = angle.toRadians();
            }

            // Calculate the direction using trigonometric functions
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            return new Vector3(x, 0, z);
        }

        public static float GetAngleToFront180(this Vector3 FromGlobalPosition, Node3D Target, bool useRadians = false)
        {
            return GetAngleToFront180(
                GetYRotation(FromGlobalPosition - Target.GlobalPosition, useRadians),
                useRadians ? Target.Rotation.Y : Target.RotationDegrees.Y,
                useRadians
                );
        }

        public static float GetAngleToFront180(this float fromAngle, float toAngle, bool useRadians = false)
        {
            // Convert angles to radians if needed
            if (useRadians == false)
            {
                fromAngle = fromAngle.toRadians();
                toAngle = toAngle.toRadians();
            }

            // Calculate the difference between the angles
            float delta = toAngle - fromAngle;

            // Wrap the delta within the range of -Pi to Pi (or -180 to 180 degrees)
            delta = (delta + Mathf.Pi) % (Mathf.Pi * 2);

            // Ensure the result is in the range of 0 to 180 degrees and inverted
            delta = Mathf.Abs(delta);
            if (delta > Mathf.Pi)
                delta = 2 * Mathf.Pi - delta;
            delta = Mathf.Pi - delta;

            // Convert delta back to degrees if needed
            return useRadians ? delta : delta.toDegrees();
        }

        /// <summary>
        /// Returns angle from Vector2.Right. In Degrees.
        /// </summary>
        public static float GetAngleD(this Vector2 direction) => GetAngle(direction).toDegrees();

        /// <summary>
        /// Returns angle from Vector2.Right. In Radians.
        /// </summary>
        public static float GetAngle(this Vector2 direction) => direction.Normalized().Angle();

        /// <summary>
        /// Returns direction from Vector2.Right. In Degrees.
        /// </summary>
        public static Vector2 GetDirectionD(this float degrees) => GetDirection(degrees.toRadians());

        /// <summary>
        /// Returns direction from Vector2.Right. In Degrees.
        /// </summary>
        public static Vector2 GetDirection(this float radians) => Vector2.Right.Rotated(radians).Normalized();
        #endregion

        #region Node Functions          ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void Clear(this Node parent, bool forceInstant = false)
        {
            if (parent.IsNull()) return;

            foreach (Node child in parent.GetChildren())
            {
                if (child.IsNull()) continue;

                child.Destroy(forceInstant);
            }
        }

        public static async void Destroy(this Node node, float lifeTime, bool forceInstant = false)
        {
            await Task.Delay(Mathf.RoundToInt(lifeTime * 1000));

            if (node.NotNull())
            {
                lock (node)
                {
                    Destroy(node, forceInstant);
                }
            }
        }

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

        public static T Instantiate<T>(this T prefab, Node root) where T : Node
        {
            if (prefab == null) return null;

            T t = (T)prefab.Duplicate();
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

        public static List<T> GetNodesInChildren<T>(this Node node, bool includeSelf = true, byte layerDepth = 0) where T : Node
        {
            List<T> list = new();
            loop(node, includeSelf, 0);
            return list;

            void loop(Node node, bool includeSelf, byte layer)
            {
                if (includeSelf) add(node);

                foreach (Node n in node.GetChildren())
                {
                    if (layerDepth < 1 || layer < layerDepth)
                    {
                        loop(n, true, (byte)(layer + 1));
                    }
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
                if (node is not T) return false;

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

        #region Signal Functions        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

        #region Bit Functions           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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

        #region Array Functions         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static ushort Size<T>(this T[] array) => array == null ? (ushort)0 : (ushort)array.Length;

        public static bool Compare<T>(this T[] array, T[] other)
        {
            if (array.Size() == other.Size())
            {
                if (array.Size() > 0)
                {
                    List<T> list = new(other);
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (list.Contains(array[i]) == false) return false;

                        list.Remove(array[i]);
                    }
                }

                return true;
            }

            return false;
        }

        public static bool NotEmpty<T>(this T[] array)
        => array != null && array.Length > 0;

        public static bool IsEmpty<T>(this T[] array)
        => !NotEmpty(array);

        public static T RandomElement<T>(this T[] array, T @default = default)
        => array.NotEmpty() ? array[Random.RangeInt(0, array.Length)] : @default;

        public static T GetClampedElement<T>(this T[] array, int index)
        => array.IsEmpty() ? default : array[Mathf.Clamp(index, 0, array.Length - 1)];

        public static bool Contains<T>(this T[] array, T element)
        => array != null && array.Length > 0 && ((ICollection<T>)array).Contains(element);

        public static T[] AddToArray<T>(this T[] array, T value)
        {
            AddToArray(value, ref array);
            return array;
        }

        public static void AddToArray<T>(this T element, ref T[] array)
        {
            if (array == null)
            {
                array = new T[1] { element };
                return;
            }

            T[] _array = new T[array.Length + 1];

            System.Array.Copy(array, _array, array.Length);
            _array[array.Length] = element;

            array = _array;
        }

        public static void RemoveNull<T>(ref T[] array)
        {
            if (array.IsEmpty()) return;

            List<T> list = new();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null)
                {
                    list.Add(array[i]);
                }
            }

            array = list.ToArray();
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array.Size() >= index) return array;

            var result = new T[array.Length - 1];

            System.Array.Copy(array, result, index);
            System.Array.Copy(array, index + 1, result, index, array.Length - index - 1);

            return result;
        }

        public static T[] RemoveFromArray<T>(this T[] array, T value)
        {
            RemoveFromArray(value, ref array);
            return array;
        }

        public static void RemoveFromArray<T>(this T element, ref T[] array, bool removeAllOccurences = false)
        {
            if (array.IsEmpty()) return;

            List<T> list = new();
            bool removed = false;

            for (int i = 0; i < array.Length; i++)
            {
                if ((removed && removeAllOccurences == false) || array[i].Equals(element) == false)
                {
                    list.Add(array[i]);
                }

                else
                {
                    removed = true;
                }
            }

            array = list.ToArray();
        }

        public static T[] OffsetElements<T>(this T[] array, int offset)
        {
            OffsetElements(ref array, offset);
            return array;
        }

        public static void OffsetElements<T>(ref T[] array, int offset)
        {
            T[] result = new T[array.Length];

            // Calculate the effective offset (taking negative offsets into account)
            offset = -offset % array.Length;

            if (offset < 0) offset += array.Length;
            else if (offset == 0) return;

            // Copy the bytes to the result array with the offset
            System.Array.Copy(array, offset, result, 0, array.Length - offset);
            System.Array.Copy(array, 0, result, array.Length - offset, offset);

            array = result;
        }

        public static T[] Duplicate<T>(this T[] array)
        {
            var result = new T[array.Length];

            System.Array.Copy(array, result, array.Length);

            return result;
        }
        #endregion

        #region List Functions          ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static bool NotEmpty<T>(this List<T> list) => list != null && list.Count > 0;
        public static bool IsEmpty<T>(this List<T> list) => !NotEmpty(list);

        public static T RandomElement<T>(this List<T> list, T @default = default) => list.NotEmpty() ? list[Random.RangeInt(0, list.Count)] : @default;
        #endregion

        #region Dictionary Functions    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static bool NotEmpty<T, U>(this Dictionary<T, U> dic) => dic != null && dic.Count > 0;
        public static bool IsEmpty<T, U>(this Dictionary<T, U> dic) => !NotEmpty(dic);

        /// <summary>
        /// Sets entry no matter if key is already contained.
        /// </summary>
        public static void Set<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if ((dic ??= new()).ContainsKey(key))
            {
                dic[key] = value;
            }

            else
            {
                dic.Add(key, value);
            }
        }

        /// <summary>
        /// Adds key, value to dictionary if not contained.
        /// </summary>
        public static bool TryAdd<K, V>(this Dictionary<K, V> dic, K key, V value)
        {
            if ((dic ??= new()).ContainsKey(key))
            {
                return false;
            }

            else
            {
                dic.Add(key, value);
                return true;
            }
        }

        /// <summary>
        /// Removes key from dictionary if contained.
        /// </summary>
        public static bool TryRemove<K, V>(this Dictionary<K, V> dic, K key)
        {
            if ((dic ??= new()).ContainsKey(key) == false)
            {
                return false;
            }

            else
            {
                dic.Remove(key);
                return true;
            }
        }
        #endregion

        #region Float Functions         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float abs(this float f) => Math.Abs(f);

        public static float max(this float f0, float f1) => Math.Max(f0, f1);
        public static float max(this float f0, float f1, float f2) => max(max(f0, f1), f2);
        public static float max(this float f0, float f1, float f2, float f3) => max(max(f0, f1, f2), f3);


        public static float min(this float f0, float f1) => Math.Min(f0, f1);
        public static float min(this float f0, float f1, float f2) => min(min(f0, f1), f2);
        public static float min(this float f0, float f1, float f2, float f3) => min(min(f0, f1, f2), f3);
        #endregion

        #region Integer Functions       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static int abs(this int i) => Math.Abs(i);

        public static int max(this int f0, int f1) => Math.Max(f0, f1);
        public static int max(this int f0, int f1, int f2) => max(max(f0, f1), f2);
        public static int max(this int f0, int f1, int f2, int f3) => max(max(f0, f1, f2), f3);

        public static int min(this int f0, int f1) => Math.Min(f0, f1);
        public static int min(this int f0, int f1, int f2) => min(min(f0, f1), f2);
        public static int min(this int f0, int f1, int f2, int f3) => min(min(f0, f1, f2), f3);
        #endregion

        #region Vector2 Functions       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Vector2 Lerp(this Vector2 a, Vector2 b, float lerp) => new(Mathf.Lerp(a.X, b.X, lerp), Mathf.Lerp(a.Y, b.Y, lerp));

        public static Vector2 setX(this Vector2 v2, float value) => new(value, v2.Y);
        public static Vector2 setY(this Vector2 v2, float value) => new(v2.X, value);


        public static void pasteX(this float value, ref Vector2 v2) => v2.X = value;
        public static void pasteY(this float value, ref Vector2 v2) => v2.Y = value;

        public static Vector2I RoundToInt(this Vector2 v2) => new(Mathf.RoundToInt(v2.X), Mathf.RoundToInt(v2.Y));
        public static Vector2I FloorToInt(this Vector2 v2) => new(Mathf.FloorToInt(v2.X), Mathf.FloorToInt(v2.Y));
        public static Vector2I CeilToInt(this Vector2 v2) => new(Mathf.CeilToInt(v2.X), Mathf.CeilToInt(v2.Y));
        public static Vector2 Abs(this Vector2 v2) => new(Mathf.Abs(v2.X), Mathf.Abs(v2.Y));
        public static Vector2 Max(this Vector2 o, Vector2 a, Vector2 b) => o.DistanceTo(a) > o.DistanceTo(b) ? a : b;
        public static Vector2 Min(this Vector2 o, Vector2 a, Vector2 b) => o.DistanceTo(a) < o.DistanceTo(b) ? a : b;
        public static Vector2 NoNaN(this Vector2 v2) => new(float.IsNaN(v2.X) ? 0 : v2.X, float.IsNaN(v2.Y) ? 0 : v2.Y);

        public static Vector2 toXY(this Vector3 value) => new(value.X, value.Z);
        #endregion

        #region Vector3 Functions       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static void SetForward(this Node3D node, Vector3 direction, bool global = true) => node.LookAt((global ? node.GlobalPosition : node.Position) + direction);

        public static Vector3 setX(this Vector3 v3, float value) => new(value, v3.Y, v3.Z);
        public static Vector3 setY(this Vector3 v3, float value) => new(v3.X, value, v3.Z);
        public static Vector3 setZ(this Vector3 v3, float value) => new(v3.X, v3.Y, value);
        public static Vector3 multX(this Vector3 v3, float value) => new(v3.X * value, v3.Y, v3.Z);
        public static Vector3 multY(this Vector3 v3, float value) => new(v3.X, v3.Y * value, v3.Z);
        public static Vector3 multZ(this Vector3 v3, float value) => new(v3.X, v3.Y, v3.Z * value);

        public static void pasteX(this float value, ref Vector3 v3) => v3.X = value;
        public static void pasteY(this float value, ref Vector3 v3) => v3.Y = value;
        public static void pasteZ(this float value, ref Vector3 v3) => v3.Z = value;

        public static Vector3 toRight(this Vector3 forward) => toRight(forward, Vector3.Up);
        public static Vector3 toRight(this Vector3 forward, Vector3 up) => forward.Normalized().Cross(up.Normalized());

        public static Vector3 toUp(this Vector3 forward) => toRight(forward, Vector3.Right);
        public static Vector3 toUp(this Vector3 forward, Vector3 right) => -forward.Normalized().Cross(right.Normalized());

        public static Vector3 toXZ(this Vector2 value, float y = 0) => new(value.X, y, value.Y);

        /// <summary>
        /// Round Vector3 to given decimal spaces
        /// </summary>
        public static Vector3 Round(this Vector3 value, byte decimalSpaces = 0)
        => new(value.X.Round(decimalSpaces), value.Y.Round(decimalSpaces), value.Z.Round(decimalSpaces));

        /// <summary>
        /// Round Vector3 to given decimal spaces
        /// </summary>
        public static Vector3 Round(this Vector3 value, float step = 1f)
        => new(value.X.Round(step), value.Y.Round(step), value.Z.Round(step));

        public static float Round(this float value, byte decimalSpaces)
        => Mathf.RoundToInt(value * Mathf.Pow(10, decimalSpaces)) / Mathf.Pow(10, decimalSpaces);

        public static float Round(this float value, float step = 1f)
        {
            if (step <= 0) throw new ArgumentException("Step must be greater than zero.");

            float remainder = (value = Mathf.Ceil(value / 0.001f) * 0.001f) % step;
            float halfStep = step / 2f;

            return
                remainder >= halfStep ? value + step - remainder :
                remainder < -halfStep ? value - step - remainder :
                value - remainder;
        }
        #endregion

        #region List Functions          ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
            List<Vector3> result = new();
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
            List<Vector2> result = new();
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

        #region Input Functions         ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static float GetInputValue(this string name) => Input.GetActionRawStrength(name);

        public static Vector2 GetInputVector(this string negativeX, string positiveX, string negativeY, string positiveY) => new(GetInputAxis(negativeX, positiveX), GetInputAxis(negativeY, positiveY));
        public static float GetInputAxis(this string negative, string positive) => -negative.GetInputValue() + positive.GetInputValue();
        public static bool GetInput(this string name, float threshold = .2f) => GetInputValue(name) >= threshold;
        #endregion

        #region String Functions        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static string RemoveForbiddenDbChars(this string source) => RemoveChar(source, ' ', '#', '\'', '`', '\'', '@', '/', '\\');

        /// <summary>
        /// Returns string without listed char values
        /// </summary>
        public static string RemoveChar(this string source, params char[] chars)
        {
            if (chars.IsEmpty()) return source;

            ICollection<char> list = chars;
            return new((
                    from c in source where !list.Contains(c) select c
                ).ToArray());
        }

        /// <summary>
        /// Returns only listed char values in string
        /// </summary>
        public static string KeepChar(this string source, params char[] chars)
        {
            if (chars.IsEmpty()) return source;

            ICollection<char> list = chars;
            return new((
                    from c in source where list.Contains(c) select c
                ).ToArray());
        }

        /// <summary>
        /// Splits string after first instance of seperator
        /// </summary>
        public static string[] SplitOnce(this string source, params char[] serperators)
        {
            if (serperators.IsEmpty() || source.IsEmpty()) return null;

            for (int i = 0; i < serperators.Length; i++)
            {
                // Found a char
                if (source.Contains(serperators[i]))
                {
                    break;
                }

                // No char has been found
                if (i >= serperators.Length - 1)
                {
                    return null;
                }
            }

            // Find split index
            ICollection<char> list = serperators;
            for (int i = 0; i < source.Length - 1; i++)
            {
                if (list.Contains(source[i]))
                {
                    // Define before and after
                    string before = source[..i].Trim();
                    string after = source[(i + 1)..].Trim();

                    // Validate before and after
                    if (before.NotEmpty() && after.NotEmpty())
                    {
                        return new string[2] { before, after };
                    }

                    // Before or after is empty
                    else break;
                }
            }

            return null;
        }

        /// <summary>
        /// Trims spaces to avoid double and more spaces
        /// </summary>
        public static string TrimSpaces(this string source)
        {
            StringBuilder result = new();
            source = source.Trim();

            bool wasSpace = false;
            bool isSpace;

            // Enumerate through source
            for (int i = 0; i < source.Length; i++)
            {
                // Check if is space
                isSpace = source[i] == ' ';

                // Check if was space before
                if (isSpace == false || wasSpace == false)
                {
                    result.Append(source[i]);
                }

                // Set past to present
                wasSpace = isSpace;
            }

            return result.ToString();
        }

        /// <summary>
        /// Splits string into lines and executes function that returns the modified line
        /// </summary>
        public static string[] SplitAnd(this string source, char seperator, Func<string, string> actionPerLine, bool ignoreEmptyEntries = false)
        {
            string[] splits = source.Split(seperator, StringSplitOptions.TrimEntries);
            List<string> lines = new();

            for (int i = 0; i < splits.Length; i++)
            {
                if (ignoreEmptyEntries)
                {
                    string line = actionPerLine.Invoke(splits[i]);
                    if (line.NotEmpty())
                    {
                        lines.Add(line);
                    }
                }

                else lines.Add(actionPerLine.Invoke(splits[i]));
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Insert value in front of keys
        /// </summary>
        public static string InsertInFrontOf(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();

            ICollection<char> splits = keys;
            for (ushort i = 0; i < source.Length; i++)
            {
                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                }

                stringBuilder.Append(source[i]);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Insert value in front of and after keys
        /// </summary>
        public static string InsertInFrontOfAndAfter(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();

            ICollection<char> splits = keys;
            for (ushort i = 0; i < source.Length; i++)
            {
                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                    stringBuilder.Append(source[i]);
                    stringBuilder.Append(insertValue);
                }

                else stringBuilder.Append(source[i]);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Inserts value after keys
        /// </summary>
        public static string InsertAfter(this string source, string insertValue, params char[] keys)
        {
            if (source.IsEmpty()) return source;
            StringBuilder stringBuilder = new();
            stringBuilder.Append(source[0]);

            ICollection<char> splits = keys;
            for (ushort i = 1; i < source.Length; i++)
            {
                stringBuilder.Append(source[i]);

                if (splits.Contains(source[i]))
                {
                    stringBuilder.Append(insertValue);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Removes empty lines
        /// </summary>
        public static string RemoveEmptyLines(this string source)
        {
            if (source.IsEmpty()) return source;

            string[] lines = source.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            StringBuilder stringBuilder = new();

            stringBuilder.Append(lines[0]);
            for (ushort i = 1; i < lines.Length; i++)
            {
                stringBuilder.Append($"\n{lines[i]}");
            }

            return stringBuilder.ToString();
        }

        public static string RemoveBehind(this string source, string identifier)
        {
            if (identifier.IsEmpty() || source.IsEmpty() || source.Contains(identifier[0]) == false || source.Contains(identifier) == false) return source;

            return source.Split(identifier, StringSplitOptions.TrimEntries)[0];
        }

        public static string TrimToDirectory(this string path)
        {
            if (string.IsNullOrEmpty(path) || !(path.Contains('/') || path.Contains('\\'))) return path;

            char c;
            for (int i = path.Length; i > 0; i--)
            {
                c = path[i - 1];

                if (c.Equals('\\') || c.Equals('/'))
                {
                    return path[..(i - 1)];
                }
            }

            return path;
        }

        public static string ReplaceFirst(this string str, char c, object value)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!str[i].Equals(c)) continue;

                str = str[..i++] + value + str[i..];
                break;
            }

            return str;
        }

        public static string Fill(this string str, int targetLength, char fillChar, bool before = true)
        {
            if (targetLength < 1 || (str = str.Trim()).Length >= targetLength) return str;

            StringBuilder builder = new();
            builder.Append(str);

            for (int i = builder.Length; i < targetLength; i++)
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

            StringBuilder result = new();

            List<string> _extracted = new();
            StringBuilder str = new();
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

        #region Enum Functions          ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Array Array(this Enum Enum) => Enum.GetValues(Enum.GetType());
        public static int Length(this Enum Enum) => Array(Enum).Length;

        public static Array EnumArray<T>() where T : Enum => Enum.GetValues(typeof(T));
        public static int EnumLength<T>() where T : Enum => EnumArray<T>().Length;
        #endregion

        #region Queue Actions           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static async void QueueAction(this Node node, Action action, float timeInSeconds)
        {
            await Task.Delay(Mathf.RoundToInt(timeInSeconds * 1000));

            if (node.NotNull())
            {
                lock (node)
                {
                    action.Invoke();
                }
            }
        }

        public static async void QueueAction(this Action action, float timeInSeconds)
        {
            await Task.Delay(Mathf.RoundToInt(timeInSeconds * 1000));

            if (action != null)
            {
                lock (action)
                {
                    action.Invoke();
                }
            }
        }
        #endregion
    }
}
