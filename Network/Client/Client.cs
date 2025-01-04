namespace Cutulu.Network
{
    using System.Threading.Tasks;
    using System;

    using Protocols;
    using Sockets;
    using Core;

    public partial class Client
    {
        public readonly TcpSocket TcpClient;
        public readonly UdpSocket UdpClient;

        public string Address { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public long UID { get; private set; }

        private byte ThreadIdx { get; set; }

        public bool IsConnected => TcpClient != null && TcpClient.IsConnected;

        public Action<short, byte[]> Received;
        public Action Connected, Disconnected;

        public Client(string address, int tcpPort, int udpPort)
        {
            Address = address;
            TcpPort = tcpPort;
            UdpPort = udpPort;

            TcpClient = new()
            {
                Connected = ConnectEvent,
                Disconnected = DisconnectEvent,
            };

            UdpClient = new();
        }

        #region Callable Functions

        /// <summary>
        /// Starts client.
        /// </summary>
        public virtual async Task Start()
        {
            await Stop();

            ThreadIdx++;

            await UdpClient.Connect(Address, UdpPort);

            await TcpClient.Connect(Address, TcpPort);
        }

        /// <summary>
        /// Stops client.
        /// </summary>
        public virtual async Task Stop()
        {
            TcpClient.Disconnect();
            UdpClient.Disconnect();

            while (TcpClient.IsConnected || UdpClient.IsConnected) await Task.Delay(1);
            await Task.Delay(1);
        }

        /// <summary>
        /// Sends data to host.
        /// </summary>
        public virtual void Send(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) TcpClient.Send(length.Encode(), packet);
                else UdpClient.Send(packet);
            }
        }

        /// <summary>
        /// Sends data to host async.
        /// </summary>
        public virtual async Task SendAsync(short key, object obj, bool reliable = true)
        {
            if (IsConnected)
            {
                var packet = PacketProtocol.Pack(key, obj, out var length);

                if (reliable) await TcpClient.SendAsync(length.Encode(), packet);
                else await UdpClient.SendAsync(packet);
            }
        }

        public virtual void Receive(short key, byte[] buffer) { }

        #endregion

        #region Event Handlers

        private async void ConnectEvent(TcpSocket socket)
        {
            await socket.SendAsync(new[] { (byte)ConnectionTypeEnum.Connect }, UdpClient.GetLocalEndpoint().Port.Encode());

            var (Success, Buffer) = await socket.Receive(1);
            if (Success == false || Buffer[0] != 1)
            {
                Debug.LogR($"[color=indianred][{GetType().Name}] Failed to connect to host.");

                await Stop();
                return;
            }

            (Success, Buffer) = await socket.Receive(8);
            if (Success == false)
            {
                Debug.LogR($"[color=indianred][{GetType().Name}] Failed to read UID.");

                await Stop();
                return;
            }

            UID = Buffer.Decode<long>();

            lock (this) Connected?.Invoke();
            ReceiveData();
        }

        private void DisconnectEvent(TcpSocket socket)
        {
            lock (this) Disconnected?.Invoke();
        }

        /// <summary>
        /// Defines the logic to receive data from host.
        /// </summary>
        protected virtual async void ReceiveData()
        {
            if (TcpClient.IsConnected == false) return;
            var threadIdx = ThreadIdx;

            udp();
            async void udp()
            {
                var packet = await UdpClient.Receive();
                if (active() == false) return;

                if (packet.Success && PacketProtocol.Unpack(packet.Buffer, out var key, out var buffer))
                {
                    Receive(key, buffer);
                    Received?.Invoke(key, buffer);
                }

                udp();
            }

            // Receive tcp packages and disconnect if they cannot be received anymore
            while (active())
            {
                var packet = await TcpClient.Receive(4);

                if (packet.Success == false || active() == false) continue;

                packet = await TcpClient.Receive(packet.Buffer.Decode<int>());
                if (active() == false) continue;

                if (packet.Success && PacketProtocol.Unpack(packet.Buffer, out var key, out var buffer))
                {
                    lock (this)
                    {
                        Receive(key, buffer);
                        Received?.Invoke(key, buffer);
                    }
                }
            }

            bool active() => TcpClient.IsConnected && UdpClient.IsConnected && ThreadIdx == threadIdx;

            // Stop if disconnected
            await Stop();
        }

        #endregion
    }
}