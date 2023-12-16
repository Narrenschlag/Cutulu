using System.Collections.Generic;
using Cutulu;

namespace Walhalla.Server
{
    public class ClientBase
    {
        public delegate void PacketReceive(byte key, BufferType type, byte[] bytes, bool tcp);
        public delegate void Disconnect(ClientBase client);

        public PacketReceive onReceive;
        public Disconnect onClose;

        protected Dictionary<uint, ClientBase> Registry;
        public uint UID;

        public ClientBase(uint uid, ref Dictionary<uint, ClientBase> registry, PacketReceive onReceive)
        {
            $"+++ Connected [{uid}]".Log();

            Registry = registry;
            UID = uid;

            this.onReceive = onReceive;
        }

        public virtual void send(byte key, BufferType type, byte[] bytes, bool tcp) { }
        public virtual void send<T>(byte key, T value, bool tcp) { }

        /// <summary> Handles incomming traffic </summary>
        public virtual void _receive(byte key, BufferType type, byte[] bytes, bool tcp)
        {
            $"{UID}> {(tcp ? "tcp" : "udp")}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
            if (onReceive != null) onReceive(key, type, bytes, tcp);
        }

        public virtual void _disconnect()
        {
            $"--- Disconnected [{UID}]".Log();

            if (onClose != null)
                onClose(this);

            lock (Registry)
            {
                Registry.Remove(UID);
            }
        }
    }
}