using System.Collections.Generic;
using Cutulu;

namespace Walhalla.Server
{
    public class ClientBase
    {
        public delegate void Disconnect(ClientBase client);

        public Delegates.Packet onReceive;
        public Disconnect onClose;

        protected Dictionary<uint, ClientBase> Registry;
        public uint UID;

        public ClientBase(uint uid, ref Dictionary<uint, ClientBase> registry, Delegates.Packet onReceive)
        {
            Registry = registry;
            UID = uid;

            this.onReceive = onReceive;

            $"+++ Connected [{UID}]".Log();
        }

        public virtual void send(byte key, BufferType type, byte[] bytes, Method method) { }
        public virtual void send<T>(byte key, T value, Method method) { }

        /// <summary> Handles incomming traffic </summary>
        public virtual void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            //$"{UID}> {(tcp ? "tcp" : "udp")}-package: {key} ({type}, {(bytes == null ? 0 : bytes.Length)})".Log();
            if (onReceive != null) onReceive(key, type, bytes, method);
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