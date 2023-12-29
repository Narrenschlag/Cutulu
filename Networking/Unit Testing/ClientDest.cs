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

		public override void __receive(byte key, BufferType type, byte[] bytes, Method method, params object[] values)
		{
			base.__receive(key, type, bytes, method, values);

			if (output.NotNull())
			{
				if (type == BufferType.String && bytes.As(out string txt))
				{
					txt = $"{key}> {txt}";

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
					output.Text += $"key({key}) {bytes.Length} bytes\n";
				}
			}
		}
	}
}