using System;

namespace Cutulu
{
    public class Protocol
    {
        public delegate void Packet(byte key, byte[] bytes, Method method);
        public delegate void Empty();

        public bool Connected;
        public int Port;

        public Protocol(int port)
        {
            Port = port;
        }

        /// <summary> Closes handler </summary>
        public virtual void Close()
        {
            Connected = false;
        }

        #region Send Data
        /// <summary>
        /// Valdiates connection
        /// </summary>
        protected void ValidateConnection()
        {
            if (Connected == false) throw new Exception($"{GetType()} is not connected");
        }
        #endregion
    }
}