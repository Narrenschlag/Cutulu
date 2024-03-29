using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Cutulu
{
    class ProxyServer
    {
        private TcpListener listener;
        private string targetHost;
        private int targetPort;

        public ProxyServer(string listenAddress, int listenPort, string targetHost, int targetPort)
        {
            listener = new TcpListener(IPAddress.Parse(listenAddress), listenPort);
            this.targetHost = targetHost;
            this.targetPort = targetPort;
        }

        public async Task Start()
        {
            listener.Start();
            Debug.Log($"Proxy server started. Listening on {listener.LocalEndpoint}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Debug.Log($"Accepted connection from {client.Client.RemoteEndPoint}");

                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                using (TcpClient targetClient = new TcpClient())
                {
                    await targetClient.ConnectAsync(targetHost, targetPort);

                    NetworkStream clientStream = client.GetStream();
                    NetworkStream targetStream = targetClient.GetStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead;

                    while ((bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await targetStream.WriteAsync(buffer, 0, bytesRead);
                        await targetStream.FlushAsync();
                    }

                    Debug.Log($"Data forwarded from {client.Client.RemoteEndPoint} to {targetClient.Client.RemoteEndPoint}");

                    // Optionally, read response from target server and send it back to the client
                }
            }
        }
    }
}