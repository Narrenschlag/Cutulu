namespace Cutulu.Network
{
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net;

    using Sockets;
    using Core;

    public partial class IntegrationTest : Core.IntegrationTest
    {
        protected override int StepCount => 14;

        protected override async Task<bool> _Process()
        {
            const int TcpPort = 9977;

            #region Establish tcp host

            // Establish basic listener for debugging
            var HOST = new Sockets.TcpHost();
            HOST.Start(TcpPort);

            Print($"Established debug host (port={TcpPort})");
            NextStep();

            #endregion

            #region Connect tcp socket

            Print($"Starting connection process via tcp.");

            var CLIENT = new TcpSocket();

            // Connect client to debug host
            if (await CLIENT.Connect(IO.LocalhostIPv6, TcpPort, 1000) == false)
            {
                PrintErr($"Timed out on connecting to debug host");
                return false;
            }

            Print("Connected tcp client to debug host.");
            NextStep();

            #endregion

            #region Reconnect tcp socket

            Print($"Starting reconnection process via tcp.");

            // Connect client to debug host
            if (await CLIENT.Connect(IO.LocalhostIPv6, TcpPort, 1000) == false)
            {
                PrintErr($"Timed out on connecting to debug host");
                return false;
            }

            Print("Reconnected tcp client to debug host.");
            NextStep();

            #endregion

            #region Accept tcp client on tcp host

            // Very important to take latency into account
            await Task.Delay(10);

            Print($"Listening to incomming connections... ({HOST.Sockets.Count})");

            var socket = HOST.Sockets.Values.ToList()[^1];

            if (socket == null)
            {
                PrintErr($"No socket has been found.");
                return false;
            }

            Print($"Connected with client.");
            NextStep();

            #endregion

            #region Send and Receive data between host and socket

            Print("Client sends...");
            var writeBuffer = "I hate this damn socket system.".Encode();

            if (await CLIENT.SendAsync(writeBuffer) == false)
            {
                PrintErr($"Unable to send data. (client -> host)");
                return false;
            }

            Print("Reading client data...");
            var readBuffer = (await socket.Receive(writeBuffer.Length)).Buffer;

            if (readBuffer.Compare(writeBuffer) == false)
            {
                PrintErr($"{writeBuffer.Decode<string>()} != {readBuffer.Decode<string>()}.");
                return false;
            }

            else Print($"Success. '{readBuffer.Decode<string>()}'");

            Print("Sending packages...");
            await socket.SendAsync(900090909.Encode());

            Print("Receiving packages...");
            var receive = await CLIENT.Receive(4);

            if (receive.Success == false || receive.Buffer.Decode<int>() != 900090909)
            {
                PrintErr("Unable to receive package data");
                return false;
            }

            Print($"Received all packages ({receive.Buffer.Length}, {receive.Buffer.Decode<int>()})");
            NextStep();

            #endregion

            const int UdpPort = 9979;

            #region Establish udp host

            Print($"Establishing udp host (port={UdpPort})");

            var UDP_HOST = new UdpHost();
            UDP_HOST.Start(UdpPort);

            if (UDP_HOST.IsListening == false)
            {
                PrintErr($"Timed out on establishing host");
                return false;
            }

            Print($"Established udp host (port={UdpPort})");
            NextStep();

            #endregion

            #region Connect udp socket

            Print($"Starting connection process via udp.");

            var UDP = new UdpSocket(UDP_HOST);
            await UDP.Connect(IO.LocalhostIPv6, UdpPort);

            if (UDP.IsConnected == false)
            {
                PrintErr($"Timed out on connecting to host");
                return false;
            }

            Print("Connected udp client to host.");
            NextStep();

            #endregion

            #region Send and Receive data between host and socket

            IPEndPoint lastIp = default;
            var hostReceived = 0;

            UDP_HOST.Received = (IPEndPoint ip, byte[] buffer) =>
            {
                hostReceived++;
                lastIp = ip;
            };

            Print("Sending udp packages...");

            UDP.Send(1000.Encode());
            await UDP.SendAsync(1000.Encode());

            Print("Receiving udp packages...");

            await Task.Delay(500);

            if (hostReceived < 2)
            {
                PrintErr($"Host did not receive packages. {hostReceived}/2");
                return false;
            }

            Print($"Host received {hostReceived} packages.");
            NextStep();

            var clientReceived = 0;

            UDP_HOST.Listener.Send(new[] { lastIp }, 1000.Encode());
            Print($"sent={await UDP_HOST.Listener.SendAsync(new[] { lastIp }, 1000.Encode())}");

            await Task.Delay(500);

            for (int i = 0; i < 2; i++)
            {
                if ((await UDP.Receive()).Success)
                {
                    clientReceived++;
                }
            }

            if (clientReceived < 2)
            {
                PrintErr($"Client did not receive packages. {clientReceived}/2");
                return false;
            }

            Print($"Received all packets. host -> client ({clientReceived})");
            NextStep();

            #endregion

            Debug.LogR($"[color=gold][b] --- Socket Test Completed --- ");

            #region Close all

            UDP.Disconnect();
            UDP_HOST.Stop();

            CLIENT.Disconnect();
            HOST.Stop();

            await Task.Delay(5);

            Print($"Closed all clients and hosts.");
            NextStep();

            #endregion

            #region Establish managers 

            var host = new HostManager(TcpPort, UdpPort);
            var client = new ClientManager(IO.LocalhostIPv6, TcpPort, UdpPort);

            host.Start();

            await Task.Delay(500);

            await client.Start();

            Print($"Established and started managers.");

            await Task.Delay(500);

            if (host.Connections.Count < 1)
            {
                PrintErr($"Host did not establish any connections.");
                return false;
            }

            Print("Client connected to host.");
            NextStep();

            #endregion

            #region Send and Receive tcp data between host and socket

            Print($"Sending tcp packets");

            var clientReference = $"Hello sir, are you the client?";
            var lastReceivedClient = "";

            var hostReference = $"Hello sir, are you the server?";
            var lastReceivedHost = "";

            host.Received = (connection, key, buffer) => lastReceivedHost = buffer.Decode<string>();
            client.Received = (key, buffer) => lastReceivedClient = buffer.Decode<string>();

            client.SendTcp(0, hostReference);
            host.SendTcp(host.Connections.Values.ToArray()[^1], 0, clientReference);

            await Task.Delay(100);

            if (lastReceivedClient != clientReference)
            {
                PrintErr($"Client did not receive correct packet.");
                return false;
            }

            Print($"Client Received: {lastReceivedClient}");

            if (lastReceivedHost != hostReference)
            {
                PrintErr($"Host did not receive correct packet.");
                return false;
            }

            Print($"Host Received: {lastReceivedHost}");

            NextStep();

            #endregion

            #region Send and Receive udp data between host and socket

            Print($"Sending udp packets");

            clientReference = $"Hello sir, are you the client? Udp is my name.";
            lastReceivedClient = "";

            hostReference = $"Hello sir, are you the server? Udp is my name.";
            lastReceivedHost = "";

            host.Received = (connection, key, buffer) => lastReceivedHost = buffer.Decode<string>();
            client.Received = (key, buffer) => lastReceivedClient = buffer.Decode<string>();

            client.SendUdp(0, hostReference);
            host.SendUdp(host.Connections.Values.ToArray()[^1], 0, clientReference);

            await Task.Delay(100);

            if (lastReceivedClient != clientReference)
            {
                PrintErr($"Client did not receive correct packet.");
                return false;
            }

            Print($"Client Received: {lastReceivedClient}");

            if (lastReceivedHost != hostReference)
            {
                PrintErr($"Host did not receive correct packet.");
                return false;
            }

            Print($"Host Received: {lastReceivedHost}");

            NextStep();

            #endregion

            Debug.LogR($"[color=gold][b] --- Layer Test Completed --- ");

            #region Stress test

            Print($"Starting random stress test...");

            host.Stop();

            await Task.Delay(100);

            host.Start();

            await Task.Delay(1000);

            Print($"Host is running: [color=green]{host.TcpHost.IsListening}");

            await client.Start();

            await Task.Delay(100);

            if (host.Connections.Count != 1)
            {
                PrintErr($"Failed random test. Host connections {host.Connections.Count} != 1");
                return false;
            }

            Print($"Random test completed. {host.Connections.Count} connections.");

            Print($"Starting reconnect test...");

            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0) client.Stop();
                else await client.Start();

                await Task.Delay(50);
            }

            await Task.Delay(500);

            if (host.Connections.Count != 1)
            {
                PrintErr($"Failed reconnect test (1/2). Host connections {host.Connections.Count} != 1");
                return false;
            }

            client.Stop();

            await Task.Delay(500);

            if (host.Connections.Count != 0)
            {
                PrintErr($"Failed reconnect test (2/2). Host connections {host.Connections.Count} != 0");
                return false;
            }

            Print($"Stress test completed. {host.Connections.Count} connections.");
            NextStep();

            #endregion

            Application.Quit();
            return true;
        }
    }
}