using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Cutulu
{
    /// <summary> 
    /// Server that handles incomming connections via TCP/UDP protocol. 
    /// Connections are only allowed to pass if their first packet sent is a fitting passkey. No passkey required if there is none set.
    /// Also able to process only expected or ignore blacklisted ip addresses
    /// </summary>
    public class GatedServerNetwork<R> : ServerNetwork<R> where R : Receiver
    {
        private readonly NetworkGatekeeper<R> Gatekeeper;

        /// <summary> Simple server that handles tcp only </summary>
        public GatedServerNetwork(ref Passkey passkey, Dictionary<string, byte> expected, int tcpPort = 5000, int udpPort = 5001, R welcomeTarget = null, bool acceptClients = true, int maxConnectionsPerTick = 32, params string[] blacklist)
        : base(tcpPort, udpPort, welcomeTarget, acceptClients, maxConnectionsPerTick)
        {
            Gatekeeper = new(passkey, OnConnectionPassed, expected, blacklist);
            Debug.Log($"Server is gated by using passkey");
        }

        /// <summary>
        /// Handles all incomming connections and assigns them to an id<br/>
        /// Then it starts listening to them
        /// </summary>
        protected override async void Auth()
        {
            // Ignore new connections
            if (!AcceptNewClients)
            {
                await Task.Delay(100);
                Auth();
            }

            // If a connection exists, the server will accept it
            TcpClient tcp = await TcpListener.AcceptTcpClientAsync();

            // Removed: lock(Clients)
            // Removed: Clients.Add()
            // Addded:  Gatekeeper.Queue()
            Gatekeeper.Queue(NewClient(ref tcp, LastUID++));

            // Welcome other clients
            Auth();
        }

        /// <summary> 
        /// Creates new tcp/udp client 
        /// </summary>
        protected override ServerConnection<R> NewClient(ref TcpClient tcp, uint uid)
        {
            ServerConnection<R> client;
            if (tcp.Client != null && tcp.Client.RemoteEndPoint != null)
            {
                if (tcp.Client.RemoteEndPoint is IPEndPoint endpoint)
                {
                    IPAddress address = endpoint.Address;

                    if (address != null)
                        lock (Queue)
                        {
                            client = new(ref tcp, uid, this, WelcomeTarget);

                            if (Queue.ContainsKey(address)) Queue[address] = client;
                            else Queue.Add(address, client);
                        }

                    else return error($"Client endpoint address is invalid", ref tcp);
                }

                else return error($"Client had not enpoint to fetch\nRemote Tcp Endpoint valid: {tcp.Client.RemoteEndPoint != null}", ref tcp);
            }

            else return error($"Client had problems setting up the udp connection:\nTcp valid: {tcp != null}\nTcp Client instance valid: {tcp != null && tcp.Client != null}", ref tcp);

            // Removed: OnClientJoin()
            return client;

            static ServerConnection<R> error(string message, ref TcpClient tcp)
            {
                message.LogError();
                tcp?.Close();
                return null;
            }
        }

        private void OnConnectionPassed(ServerConnection<R> connection)
        {
            if (connection == null) return;

            lock (Clients)
            {
                Clients.Add(connection.UUID, connection);
                OnClientJoin(connection);
            }
        }
    }
}