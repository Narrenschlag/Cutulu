namespace Cutulu.Network
{
    using System;

    using Protocols;
    using Sockets;
    using Core;
    using System.Threading.Tasks;

    public partial class ClientManager
    {
        public readonly TcpSocket TcpClient;
        public readonly UdpSocket UdpClient;

        public string Address { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        private byte ThreadIdx { get; set; }

        public Action<short, byte[]> Received;
        public Action Connected, Disconnected;

        public ClientManager(string address, int tcpPort, int udpPort)
        {
            Address = address;
            TcpPort = tcpPort;
            UdpPort = udpPort;

            TcpClient = new()
            {
                Connected = ConnectedEvent,
                Disconnected = DisconnectedEvent,
            };

            UdpClient = new();
        }

        public virtual async Task Start()
        {
            await Stop();

            Debug.Log($"Starting client manager...");
            ThreadIdx++;

            await UdpClient.Connect(Address, UdpPort);
            Debug.Log($"Started udp client...");

            await TcpClient.Connect(Address, TcpPort);
            Debug.Log($"Started tcp client...");
        }

        public virtual async Task Stop()
        {
            TcpClient.Disconnect();
            UdpClient.Disconnect();

            while (TcpClient.IsConnected || UdpClient.IsConnected) await Task.Delay(1);
            await Task.Delay(1);
        }

        private async void ConnectedEvent(TcpSocket socket)
        {
            Debug.LogR($"[color=red]Connected to host. udp: {UdpClient.GetLocalEndpoint().Port}");
            await socket.SendAsync(new[] { (byte)ConnectionTypeEnum.Connect }, UdpClient.GetLocalEndpoint().Port.Encode());

            var (Success, Buffer) = await socket.Receive(1);
            if (Success == false || Buffer[0] != 1)
            {
                Debug.LogR($"[color=indianred][{GetType().Name}] Failed to connect to host.");

                await Stop();
                return;
            }

            lock (this) Connected?.Invoke();
            ReceiveData();
        }

        private void DisconnectedEvent(TcpSocket socket)
        {
            lock (this) Disconnected?.Invoke();
        }

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

        public virtual void SendTcp(short key, object obj)
        {
            if (TcpClient.IsConnected == false) return;

            var packet = PacketProtocol.Pack(key, obj, out var length);

            TcpClient.Send(length.Encode(), packet);
        }

        public virtual void SendUdp(short key, object obj)
        {
            if (UdpClient.IsConnected == false) return;

            var packet = PacketProtocol.Pack(key, obj, out _);

            UdpClient.Send(packet);
        }

        public virtual void Receive(short key, byte[] buffer) { }
    }
}