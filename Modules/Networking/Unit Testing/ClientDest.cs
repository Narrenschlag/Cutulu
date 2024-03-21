using Godot;

namespace Cutulu.UnitTest.Network
{
    public partial class ClientDest : Receiver
    {
        [Export] private RichTextLabel output;

        public override void _EnterTree()
        {
            if (output.NotNull())
            {
                output.Text = "";
            }
        }

        public override void Receive(ref NetworkPackage package, params object[] values)
        {
            base.Receive(ref package, values);

            if (output.NotNull())
            {
                if (package.TryBuffer(out string txt))
                {
                    txt = $"{package.Method}(k:{package.Key})[{package.Content.Length}b]> {txt}";

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
                    output.Text += $"{package.Method}(k:{package.Key}) {package.Content.Length}b\n";
                }
            }
        }
    }
}