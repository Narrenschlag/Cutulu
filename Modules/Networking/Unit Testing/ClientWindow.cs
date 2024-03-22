using System.Text;
using System.Text.Unicode;
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
                Tcp.ConnectButton(this, "SendTcp");
            }

            if (Udp.NotNull())
            {
                Udp.ConnectButton(this, "SendUdp");
            }
        }

        private void SendTcp() => Send(Method.Tcp);
        private void SendUdp() => Send(Method.Udp);
        private void Send(Method method)
        {
            if (short.TryParse(Key.Text, out var key) == false) return;

            Net.Send(key, String.Text, method);
        }
    }
}