using System;

namespace Cutulu
{
    public class Protocol
    {
        public delegate void Packet(byte key, BufferType type, byte[] bytes, Method method);
        public delegate void Empty();

        public int Port;

        public Protocol(int port)
        {
            Port = port;
        }

        public virtual bool Connected() => false;

        /// <summary> Closes handler </summary>
        public virtual void Close() { }

        #region Send Data
        /// <summary> Sends data through connection </summary>
        public virtual void send<T>(byte key, T value, bool small = true) => errorValidate();

        protected void errorValidate()
        {
            if (!Connected()) throw new Exception($"{GetType()} is not connected");
        }
        #endregion
    }
}