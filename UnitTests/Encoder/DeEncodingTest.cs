#if GODOT4_0_OR_GREATER
namespace Cutulu.Core.UnitTest
{
    using System.IO;
    using System;
    using Godot;

    public partial class DeEncodingTest : Node
    {
        public override void _EnterTree()
        {
            int
            step = 0,
            steps = 5;

            var time = System.Diagnostics.Stopwatch.StartNew();

            void stepLog(string hint) => Debug.LogError($"Integration Test ___{hint}___ [{++step}/{steps}]");

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    step = 0;

                    #region Step 1
                    stepLog("MsgString");

                    Debug.Log($"Encoder Count: {BinaryEncoding.EncoderCount}");

                    var str0 = "Hello World!";
                    var buff = str0.Encode();

                    Debug.Log($"Encoded '{str0}' into {buff.Length} bytes");

                    var str1 = buff.Decode<string>();
                    Debug.Log($"{str0} => [{buff.Length} bytes] => {str1}");
                    #endregion

                    #region Step 2
                    stepLog("RandomClass");

                    var testClass0 = new TestClass0()
                    {
                        Name = "Der Narr",
                        HealthPoints = 69,
                    };

                    buff = testClass0.Encode();

                    Debug.Log($"Encoded '{testClass0.Name} ({testClass0.HealthPoints} hp)' into {buff.Length} bytes");

                    var testClass1 = buff.Decode<TestClass0>() ?? throw new("testClass1 could not be decoded.");

                    Debug.Log($"'{testClass0.Name} ({testClass0.HealthPoints} hp)' => [{buff.Length} bytes] => '{testClass1.Name} ({testClass1.HealthPoints} hp)'");
                    #endregion

                    #region Step 3
                    stepLog("Vector3Encoder");

                    var vector30 = new Vector3(1, 2, -3);

                    buff = vector30.Encode();

                    Debug.Log($"Encoded {vector30} into {buff.Length} bytes");

                    var vector31 = buff.Decode<Vector3>();
                    if (vector31 != vector30) throw new("vector3 cannot be decoded");

                    Debug.Log($"{vector30} => [{buff.Length} bytes] => {vector31}");
                    #endregion

                    #region Step 4
                    stepLog("DictionaryEncoder");

                    var dict = new System.Collections.Generic.Dictionary<string, int>()
                {
                    { "entry0", -1 },
                    { "last", 9997 },
                };

                    buff = dict.Encode();

                    Debug.Log($"Encoded {dict.GetType().Name}[{dict.Count} entries] into {buff.Length} bytes");
                    foreach (var pair in dict) Debug.LogR($"[color=magenta]> [/color]{pair.Key}: {pair.Value}");

                    if (buff.TryDecode(out System.Collections.Generic.Dictionary<string, int> dict2) == false || dict2.Count != dict.Count)
                        throw new("Dictionary<string, int> cannot be decoded");

                    foreach (var pair in dict) Debug.LogR($"[color=seagreen]> [/color]{pair.Key}: {pair.Value}");

                    Debug.LogR($"[color=seagreen]>>>[/color] Array is generic: {new int[1].GetType().IsGenericType}");
                    Debug.LogR($"[color=seagreen]>>>[/color] Array<T> is generic: {Array.Empty<TestClass0>().GetType().IsGenericType}");
                    Debug.LogR($"[color=seagreen]>>>[/color] List<T> is generic: {new System.Collections.Generic.List<int>().GetType().IsGenericType}");

                    #endregion

                    #region Step 5
                    stepLog("(,)-Encoder");

                    var abc = (69, 'P');

                    buff = abc.Encode();

                    Debug.Log($"Encoded {abc.GetType().Name} into {buff.Length} bytes");

                    if (buff.TryDecode(out (int UID, char Name) def) == false || def != abc)
                        throw new($"{abc.GetType().Name} cannot be decoded");

                    Debug.Log($"Decoded into {abc.Item1}={def.UID}, {abc.Item2}={def.Name}");

                    var ghi = (69, '+', new Vector2(-101, 101), "Name");

                    buff = ghi.Encode();

                    Debug.Log($"Encoded {ghi.GetType().Name} into {buff.Length} bytes");

                    if (buff.TryDecode(out (int UID, char Char, Vector2 V2, string Name) jkl) == false || jkl != ghi)
                        throw new($"{ghi.GetType().Name} cannot be decoded");

                    Debug.Log($"Decoded into {ghi.Item1}={jkl.UID}, {ghi.Item2}={jkl.Char}, {ghi.Item3}={jkl.V2}, {ghi.Item4}={jkl.Name}");

                    #endregion
                }

                catch (Exception ex)
                {
                    time.Stop();
                    Debug.LogError($"Failed in {time.ElapsedMilliseconds} ms [{step}/{steps}] {ex.Message}\n{ex.StackTrace}");
                    Application.Quit();
                    return;
                }
            }

            //finally
            {
                time.Stop();
                Debug.LogError($"Succeeded in {time.ElapsedMilliseconds} ms.");
                Application.Quit();
            }
        }

        private class TestClass0
        {
            public string Name { get; set; }
            public int HealthPoints { get; set; }
        }

        private class Vector3Encoder : BinaryEncoder<Vector3>
        {
            public override void Encode(BinaryWriter writer, ref object value)
            {
                var obj = (Vector3)value;

                writer.Write(obj.X);
                writer.Write(obj.Y);
                writer.Write(obj.Z);
            }

            public override object Decode(BinaryReader reader)
            {
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }
    }
}
#endif