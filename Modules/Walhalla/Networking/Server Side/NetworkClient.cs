namespace Walhalla.Server
{
    public class NetworkClient<T> : Pointer<T> where T : Target
    {
        public AdvancedClient Source;
        public Network<T> Parent;

        public NetworkClient(Network<T> parent, AdvancedClient advancedClient, T target = null) : base(0, target)
        {
            advancedClient.onClose += _disconnect;
            advancedClient.onReceive += _receive;

            Source = advancedClient;
            Parent = parent;
        }

        public virtual void Send<V>(byte key, V value, Method method = Method.Tcp)
        {
            try
            {
                Source.send(key, value, method);
            }
            catch { }
        }

        private void _disconnect(ClientBase client)
        {
            base._disconnect();

            lock (Parent)
            {
                Parent.onClientQuit(this);
            }
        }
    }
}