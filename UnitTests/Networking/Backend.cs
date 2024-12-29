namespace Colloseum.Test
{
    using System.Threading.Tasks;
    using System.Text;
    using System;
    using Godot;

    using Cutulu.Network;
    using Cutulu.Core;

    public partial class Backend : Node
    {
        public Client Client { get; private set; }
        public Host Server { get; private set; }

        public override void _Ready()
        {
            StartUnitTest();
        }

        private async void StartUnitTest()
        {
            Debug.LogError($"### [Start unit-test for Colloseum.]");

            try
            {
                Debug.LogError($"[step 0/6] host");
                Server = new();
                var received_host_tcp = 0;
                var received_host_udp = 0;
                Server.ReceivedTcp += (Connection connection, short key, byte[] buffer) => Debug.Log($"host-tcp [{connection.UID}] ({++received_host_tcp}): {key}({buffer.Length} bytes)");
                Server.ReceivedUdp += (Connection connection, short key, byte[] buffer) => Debug.Log($"host-udp [{connection.UID}] ({++received_host_udp}): {key}({buffer.Length} bytes)");

                await Server.Start(5000, 5001);

                await Task.Delay(100);
                Debug.LogError($"[test 1/6] client");

                Client = new();
                var received_client_tcp = 0;
                var received_client_udp = 0;
                Client.ReceivedTcp += (short key, byte[] buffer) => Debug.Log($"client-tcp ({++received_client_tcp}): {key}({buffer.Length} bytes)");
                Client.ReceivedUdp += (short key, byte[] buffer) => Debug.Log($"client-udp ({++received_client_udp}): {key}({buffer.Length} bytes)");
                await Client.Connect(Cutulu.Network.IO.LocalhostIPv4, 5000, 5001);

                await Task.Delay(100);

                Debug.LogError($"[test 2/6] tcp");

                await Client.WriteTcpAsync(69, Encoding.UTF8.GetBytes("Hello sir, are you the server?"));
                await Server.Connections[Client.UID].WriteTcpAsync(69, Encoding.UTF8.GetBytes("Hello sir, are you the client?"));

                Client.WriteTcp(69, Encoding.UTF8.GetBytes("Hello sir, are you the server?..."));
                Server.Connections[Client.UID].WriteTcp(69, Encoding.UTF8.GetBytes("Hello sir, are you the client?..."));

                await Task.Delay(100);

                if (received_host_tcp != 2 || received_client_tcp != 2)
                    throw new($"Failed: {received_host_tcp}/2 {received_client_tcp}/2");

                Debug.LogError($"[test 3/6] udp");

                await Client.WriteUdpAsync(69, Encoding.UTF8.GetBytes("Hello sir, are you the server?"));
                await Server.Connections[Client.UID].WriteUdpAsync(69, Encoding.UTF8.GetBytes("Hello sir, are you the client?"));

                Client.WriteUdp(69, Encoding.UTF8.GetBytes("Hello sir, are you the server?..."));
                Server.Connections[Client.UID].WriteUdp(69, Encoding.UTF8.GetBytes("Hello sir, are you the client?..."));

                await Task.Delay(100);

                if (received_host_udp != 2 || received_client_udp != 2)
                    throw new($"Failed: {received_host_udp}/2 {received_client_udp}/2");

                Server.Connections[Client.UID].WriteTcp(67, Encoding.UTF8.GetBytes("Hello sir, are you the client? (tcp"));
                Server.Connections[Client.UID].WriteUdp(65, Encoding.UTF8.GetBytes("Hello sir, are you the client? udp)"));
                await Server.Connections[Client.UID].WriteUdpAsync(64, Encoding.UTF8.GetBytes("Hello sir, are you the client? udp)"));

                await Task.Delay(250);
                Debug.LogError($"[test 4/6]");
                await Client.Disconnect();

                await Task.Delay(100);
                Debug.LogError($"[test 5/6]");
                await Client.Connect(Cutulu.Network.IO.LocalhostIPv6, 5000, 5001);

                await Task.Delay(100);
                Debug.LogError($"[test 6/6]");
                await Client.WriteTcpAsync(101, Encoding.UTF8.GetBytes("Hello sir, are you the server? (tcp"));
                await Client.WriteUdpAsync(-101, Encoding.UTF8.GetBytes("Hello sir, are you the server? udp)"));

                Server.Connections[Client.UID].WriteTcp(67, Encoding.UTF8.GetBytes("Hello sir, are you the client? (tcp"));
                Server.Connections[Client.UID].WriteUdp(65, Encoding.UTF8.GetBytes("Hello sir, are you the client? udp)"));
                await Server.Connections[Client.UID].WriteUdpAsync(64, Encoding.UTF8.GetBytes("Hello sir, are you the client? udp)"));
            }

            catch (Exception ex)
            {
                Debug.LogError($"[unit-test failed] {ex.Message}");
                return;
            }

            await Task.Delay(500);

            Debug.LogError($"[unit-test succeeded]");
        }
    }
}