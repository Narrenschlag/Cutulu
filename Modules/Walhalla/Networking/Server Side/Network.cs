
namespace Walhalla.Server
{
    public class Network<T> : AdvancedServer where T : Target
    {
        public delegate void NetClient(NetworkClient<T> client);
        public NetClient onJoin, onQuit;

        protected override ClientBase newClient(ref System.Net.Sockets.TcpClient tcp, uint uid)
        {
            AdvancedClient client = (AdvancedClient)base.newClient(ref tcp, uid);

            NetworkClient<T> nClient = new NetworkClient<T>(this, client, null);

            lock (onJoin)
                if (onJoin != null)
                {
                    onJoin(nClient);
                }

            return client;
        }
    }
}