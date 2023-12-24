using System;

namespace Walhalla
{
    public class HandlerBase
    {
        public Delegates.Packet onReceive;
        public int Port;

        public HandlerBase(int port, Delegates.Packet onReceive)
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
        public virtual void send<T>(byte key, T value, bool small = true) => conThrow();

        /// <summary> Sends data through connection </summary>
        public virtual void send(byte key, BufferType type, byte[] bytes) => conThrow();

        protected void conThrow()
        {
            if (!Connected) throw new Exception($"{GetType()} is not connected");
        }
        #endregion
    }
}