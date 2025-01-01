namespace Cutulu.Network
{
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Net;
    using System;

    using Core;

    public partial class IntegrationTest : Core.IntegrationTest
    {
        protected override int StepCount => 5;

        private Sockets.TcpSocket BaseTcp;

        protected override async Task<bool> _Process()
        {
            BaseTcp?.Close();

            const int TcpHost = 9977;

            #region Establish tcp debug host

            // Establish basic listener for debugging
            var tcpListener = new TcpListener(IPAddress.IPv6Any, TcpHost);
            try
            {
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                tcpListener.Start();
            }

            catch (Exception ex)
            {
                PrintErr(ex.Message);
                return false;
            }

            Print($"Established debug host (port={TcpHost})");
            NextStep();

            #endregion

            #region Connect tcp Client

            Print($"Starting connection process via tcp.");

            // Connect client to debug host
            if (await (BaseTcp = new()).Connect(IO.LocalhostIPv6, TcpHost, 1000, true) == false)
            {
                PrintErr($"Timed out on connecting to debug host");
                return false;
            }

            Print("Connected tcp client to debug host.");
            NextStep();

            #endregion

            #region Reconnect tcp Client

            Print($"Starting reconnection process via tcp.");

            // Connect client to debug host
            if (await (BaseTcp = new()).Connect(IO.LocalhostIPv6, TcpHost, 1000) == false)
            {
                PrintErr($"Timed out on connecting to debug host");
                return false;
            }

            Print("Reconnected tcp client to debug host.");
            NextStep();

            #endregion

            #region Accept tcp client on debug host

            Print("Listening to incomming connections...");

            TcpClient client = null;
            while (tcpListener.Pending())
            {
                client = await tcpListener.AcceptTcpClientAsync();
                Print($"Found new connection. IsConnected={client.Connected}");
            }

            if (client == null)
            {
                PrintErr($"No client has been found.");
                return false;
            }

            Print($"Connected with client.");
            NextStep();

            #endregion

            if (client.GetStream() is not NetworkStream stream)
            {
                PrintErr($"No network streeam found.");
                return false;
            }

            Print("Client sends...");
            var writeBuffer = "I hate this damn socket system.".Encode();
            await BaseTcp.SendAsync(writeBuffer);

            Print("Reading client data...");
            var readBuffer = new byte[writeBuffer.Length];
            await stream.ReadAsync(readBuffer);

            if (readBuffer.Compare(writeBuffer) == false)
            {
                PrintErr($"{writeBuffer.Decode<string>()} != {readBuffer.Decode<string>()}.");
                return false;
            }

            else Print($"Success. '{readBuffer.Decode<string>()}'");

            Print("Sending packages...");
            await stream.WriteAsync(900090909.Encode());
            await stream.FlushAsync();

            Print("Receiving packages...");
            var receive = await BaseTcp.Receive(4);

            if (receive.Success == false)
            {
                PrintErr("Unable to receive package data");
                return false;
            }

            Print($"Received all packages ({receive.Buffer.Length})");
            NextStep();

            return true;
        }
    }
}