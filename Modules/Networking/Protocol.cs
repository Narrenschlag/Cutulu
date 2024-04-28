using System.Threading;
using System;
using System.ComponentModel;

namespace Cutulu
{
    public class Protocol
    {
        public delegate void Packet(ref NetworkPackage package);
        public delegate void Empty();

        protected CancellationTokenSource CancelSource;
        protected CancellationToken Cancel;

        public bool Connected;
        public int Port;

        #region Setup           ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Setup protocol
        /// </summary>
        public Protocol(int port)
        {
            // Create a CancellationTokenSource to generate CancellationToken
            CancelSource = new();
            Cancel = CancelSource.Token;

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
                throw new WarningException($"{GetType()} is not connected.");
            }
        }
        #endregion

        #region End Connection  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Closes handler 
        /// </summary>
        public virtual void Close()
        {
            //Debug.LogError($"{GetType()} all async operations... Potentially throwing irrelevant error.");
            CancelSource?.Cancel();
            CancelSource = null;

            Connected = false;
        }
        #endregion
    }
}