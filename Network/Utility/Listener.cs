namespace Cutulu.Network
{
    /// <summary>
    /// Listener is basically a parasitic class that can be added to a connection or client manager
    /// <para>It reads incomming packets and marks them as read to block any further processing</para>
    /// </summary>
    public partial interface Listener
    {
        /// <summary>
        /// Called when a packet is received
        /// <para>Returns true if the packet should be blocked</para>
        /// </summary>
        public bool _Receive(short _key, byte[] _buffer);
    }
}