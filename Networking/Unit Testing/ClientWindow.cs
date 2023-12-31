using Godot;

namespace Cutulu.UnitTest.Network
{
	public partial class ClientWindow : Window
	{
		[Export] private ClientDest Receiver;

		[Export] private Button Tcp;
		[Export] private Button Udp;

		[Export] private LineEdit Key;
		[Export] private LineEdit String;

		private ClientNetwork<ClientDest> Net;

		public override void _EnterTree()
		{
			Net = new ClientNetwork<ClientDest>("127.0.0.1", 5000, "127.0.0.1", 5001, Receiver);

			if (Tcp.NotNull())
			{
				Tcp.ConnectButton(this, "sendTcp");
			}

			if (Udp.NotNull())
			{
				Udp.ConnectButton(this, "sendUdp");
			}
		}

		private void sendTcp()
		{
			Net.Send(byte.TryParse(Key.Text, out byte key) ? key : (byte)0, String.Text, Method.Tcp);
		}

		private void sendUdp()
		{
			Net.Send(byte.TryParse(Key.Text, out byte key) ? key : (byte)0, String.Text, Method.Udp);
		}
	}
}