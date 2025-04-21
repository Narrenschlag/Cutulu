namespace Cutulu.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;

    using Protocols;
    using Sockets;
    using Core;

    public partial class ClientManager
    {
        public readonly HashSet<Listener> Listeners = [];

        public readonly TcpSocket TcpClient;
        public readonly UdpSocket UdpClient;

        public int ConnectionTimeout { get; set; } = 5000;
        public int ValidationTimeout { get; set; } = 5000;
        public string Address { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public long UserID { get; private set; }

        private byte ThreadIdx { get; set; }

        public bool IsConnected => TcpClient != null && TcpClient.IsConnected && IsValidated;
        public bool IsValidated { get; private set; } = true;

        public Action<short, byte[]> Received;
        public Action Connected, Disconnected;

        public ClientManager()
        {
            TcpClient = new()
            {
                Connected = ConnectEvent,
                Disconnected = DisconnectEvent,
            };

            UdpClient = new();
        }

        public ClientManager(string address, int tcpPort, int udpPort) : this()
        {
            Address = address;
            TcpPort = tcpPort;
            UdpPort = udpPort;
        }

        #region Callable Functions

        /// <summary>
        /// Starts client.
        /// </summary>
        public virtual async Task<bool> Start()
        {
            await Stop(11);

            ThreadIdx++;

            await UdpClient.Connect(Address, UdpPort);
            await TcpClient.Connect(Address, TcpPort, ConnectionTimeout);

            // Validation timeout
            if (TcpClient.IsConnected)
            {
                var _token = TcpClient.Token;
                var _timeout = ValidationTimeout;

                // Wait until timed out or connection established
                while (_timeout-- > 0 && IsConnected == false && _token.IsCancellationRequested == false)
                {
                    await Task.Delay(1);
                }
            }

            if (IsConnected == false)
            {
                await Stop();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops client.
        /// </summary>
        public virtual async Task Stop()
        {
            IsValidated = false;

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

        /// <summary>
        /// Receive event, called by ReceiveBuffer.
        /// </summary>
        public virtual bool ReadPacket(short key, byte[] buffer) => false;

        /// <summary>
        /// Receive event, called by client.
        /// </summary>
        protected virtual void ReceiveBuffer(byte[] _packet_buffer)
        {
            if (PacketProtocol.Unpack(_packet_buffer, out var _key, out var _buffer))
            {
                // First let the client read the packet
                if (ReadPacket(_key, _buffer)) return;

                // Host didn't consume the packet, let the listeners read it
                foreach (var _listener in Listeners)
                    if (_listener.ReadPacket(_key, _buffer)) return;

                // No one consumed the packet, let the events read it
                Received?.Invoke(_key, _buffer);
            }
        }

        #endregion

        #region Event Handlers

        private async void ConnectEvent(TcpSocket socket)
        {
            await socket.SendAsync([(byte)ConnectionTypeEnum.Connect], UdpClient.GetLocalEndpoint().Port.Encode());

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

            UserID = Buffer.Decode<long>();
            IsValidated = true;

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

                if (packet.Success)
                {
                    lock (this)
                    {
                        ReceiveBuffer(packet.Buffer);
                    }
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

                if (packet.Success)
                {
                    lock (this)
                    {
                        ReceiveBuffer(packet.Buffer);
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