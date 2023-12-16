using System;

namespace Walhalla
{
    public class HandlerBase
    {
        public delegate void Packet(byte key, BufferType type, byte[] bytes);
        public delegate void Empty();

        public Packet onReceive;
        public int Port;

        public HandlerBase(int port, Packet onReceive)
        {
            this.onReceive = onReceive;
            Port = port;
        }

        public virtual bool Connected => false;

        /// <summary> Closes handler </summary>
        public virtual void Close()
        {
            onReceive = null;
        }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public virtual void send<T>(byte key, T value) => conThrow();

        /// <summary> Sends data through connection </summary>
        public virtual void send(byte key, BufferType type, byte[] bytes) => conThrow();

        protected void conThrow()
        {
            if (!Connected) throw new Exception($"{GetType()} is not connected");
        }
        #endregion
    }
}