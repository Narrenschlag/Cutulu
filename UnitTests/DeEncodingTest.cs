using System;
using System.IO;
using Godot;

namespace Cutulu.UnitTest
{
    public partial class DeEncodingTest : Node
    {
        public override void _EnterTree()
        {
            int
            step = 0,
            steps = 3;

            void stepLog(string hint) => Debug.LogError($"Integration Test ___{hint}___ [{++step}/{steps}]");

            try
            {
                #region Step 1
                stepLog("MsgString");

                Debug.Log($"Encoder Count: {DeEncoding.EncoderCount}");

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
            }

            catch (Exception ex)
            {
                Debug.LogError($"Failed [{step}/{steps}] {ex.Message}");
                return;
            }

            finally
            {
                Debug.LogError($"Succeeded.");
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