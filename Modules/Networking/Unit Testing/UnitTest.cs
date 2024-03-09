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

        private ServerNetwork<Receiver> Server;
        private List<ClientWindow> Windows;

        public override void _EnterTree()
        {
            base._EnterTree();

            Server = new ServerNetwork<Receiver>(5000, 5001, ServerNode.NotNull() ? ServerNode : null);

            if (ClientSide.NotNull())
            {
                ClientSide.ConnectButton(this, "OnClientSide");
            }

            if (ServerSendTcp.NotNull())
            {
                ServerSendTcp.ConnectButton(this, "OnSendServerTcp");
                ServerSendUdp.ConnectButton(this, "OnSendServerUdp");
            }
        }

        private void OnClientSide()
        {
            "Creating client".Log();

            Windows ??= new List<ClientWindow>();
            Windows.Add(ClientWindowPrefab.Instantiate<ClientWindow>(this));
        }

        private void OnSendServerTcp()
        {
            Server.Broadcast(0, "test", Method.Tcp);
        }

        private void OnSendServerUdp()
        {
            Server.Broadcast(0, "test", Method.Udp);
        }
    }
}