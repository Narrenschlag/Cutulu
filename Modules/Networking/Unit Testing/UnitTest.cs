using System.Collections.Generic;
using Godot;

namespace Cutulu.UnitTest.Network
{
    public partial class UnitTest : Node
    {
        [ExportCategory("Server")]
        [Export] private ServerNode ServerNode;
        [Export] private Button ServerSendTcp;
        [Export] private Button ServerSendUdp;

        [ExportCategory("Client")]
        [Export] private PackedScene ClientWindowPrefab;
        [Export] private Button ClientSide;

        private ServerNetwork<Destination> Server;
        private List<ClientWindow> Windows;

        public override void _EnterTree()
        {
            base._EnterTree();

            Server = new ServerNetwork<Destination>(5000, 5001, ServerNode.NotNull() ? ServerNode : null);

            if (ClientSide.NotNull())
            {
                ClientSide.ConnectButton(this, "onClientSide");
            }

            if (ServerSendTcp.NotNull())
            {
                ServerSendTcp.ConnectButton(this, "onSendServerTcp");
                ServerSendUdp.ConnectButton(this, "onSendServerUdp");
            }
        }

        private void onClientSide()
        {
            if (Windows == null)
            {
                Windows = new List<ClientWindow>();
            }

            Windows.Add(ClientWindowPrefab.Instantiate<ClientWindow>(this));
        }

        private void onSendServerTcp()
        {
            Server.Broadcast(0, "test", Method.Tcp);
        }

        private void onSendServerUdp()
        {
            Server.Broadcast(0, "test", Method.Udp);
        }
    }
}