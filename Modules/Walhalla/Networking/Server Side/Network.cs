
namespace Walhalla.Server
{
    public class Network<T> : AdvancedServer where T : Target
    {
        protected override ClientBase newClient(ref System.Net.Sockets.TcpClient tcp, uint uid)
        {
            AdvancedClient client = (AdvancedClient)base.newClient(ref tcp, uid);

            NetworkClient<T> nClient = new NetworkClient<T>(this, client, null);
            lock (this) onClientJoin(nClient);

            return client;
        }

        protected virtual void onClientJoin(NetworkClient<T> client)
        {

        }

        public virtual void onClientQuit(NetworkClient<T> client)
        {

        }
    }
}