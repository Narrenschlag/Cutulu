using System;

namespace Cutulu
{
    public class Protocol
    {
        public delegate void Packet(ref NetworkPackage package);
        public delegate void Empty();

        public bool Connected;
        public int Port;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Setup protocol
        /// </summary>
        public Protocol(int port)
        {
            Connected = false;
            Port = port;
        }
        #endregion

        #region Send Data       ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Valdiates connection
        /// </summary>
        protected void ValidateConnection()
        {
            if (Connected == false)
            {
                throw new Exception($"{GetType()} is not connected");
            }
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Closes handler 
        /// </summary>
        public virtual void Close()
        {
            Connected = false;
        }
        #endregion
    }
}