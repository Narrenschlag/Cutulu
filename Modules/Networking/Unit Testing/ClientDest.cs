using Godot;

namespace Cutulu.UnitTest.Network
{
    public partial class ClientDest : Destination
    {
        [Export] private RichTextLabel output;

        public override void _EnterTree()
        {
            if (output.NotNull())
            {
                output.Text = "";
            }
        }

        public override void Receive(byte key, byte[] bytes, Method method, params object[] values)
        {
            base.Receive(key, bytes, method, values);

            if (output.NotNull())
            {
                if (bytes.Unpack(out string txt))
                {
                    txt = $"{method}({key})[{bytes.Length}] {key}> {txt}";

                    if (output.Text.Length > 255)
                    {
                        output.Text = txt;
                    }
                    else
                    {
                        output.Text += txt + "\n";
                    }
                }
                else
                {
                    output.Text += $"{method}({key}) {bytes.Length} bytes\n";
                }
            }
        }
    }
}