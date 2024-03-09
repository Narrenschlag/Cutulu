namespace Cutulu
{
    public partial class Receiver : Godot.Node
    {
        /// <summary> 
        /// Receive Data
        /// </summary>
        public virtual void Receive(byte key, byte[] bytes, Method method, params object[] values) { }

        /// <summary> 
        /// Triggered when this is added to connection receivers
        /// </summary>
        public virtual void Add(params object[] value) { }

        /// <summary> 
        /// Triggered when this is removed from connection receivers
        /// </summary>
        public virtual void Rem(params object[] value) { }

        /// <summary> 
        /// Triggered when connection disconnects or connection is closed
        /// </summary>
        public virtual void Disconnect(params object[] values) => Rem(values);
    }
}