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

        public bool Connected;

        /// <summary> Closes handler </summary>
        public virtual void Close()
        {
            Connected = false;
        }

        #region Send Data
        /// <summary>
        /// Valdiates connection
        /// </summary>
        protected void _validateConnection()
        {
            if (Connected == false) throw new Exception($"{GetType()} is not connected");
        }
        #endregion
    }
}