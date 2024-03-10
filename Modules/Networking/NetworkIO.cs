using System.Net.Sockets;
using System.Net;

namespace Cutulu
{
    public static class NetworkIO
    {
        /// <summary>
        /// Returns address
        /// </summary>
        public static IPAddress GetAddress(this TcpClient client) => ((IPEndPoint)client.Client.RemoteEndPoint).Address;

        /// <summary>
        /// Returns port
        /// </summary>
        public static int GetPort(this TcpClient client) => ((IPEndPoint)client.Client.RemoteEndPoint).Port;

        /// <summary>
        /// Returns full address:port string and each of them as out variable
        /// </summary>
        public static string GetAddressPort(this TcpClient client, out string address, out int port)
        {
            var full = GetAddressPort(client, out IPAddress _address, out port);
            address = _address.ToString();
            return full;
        }

        /// <summary>
        /// Returns full address:port string
        /// </summary>
        public static string GetAddressPort(this TcpClient client) => GetAddressPort(client, out IPAddress _, out _);

        /// <summary>
        /// Returns full address:port string and each of them as out variable
        /// </summary>
        public static string GetAddressPort(this TcpClient client, out IPAddress address, out int port)
        {
            var endpoint = (IPEndPoint)client.Client.RemoteEndPoint;

            address = endpoint.Address;
            port = endpoint.Port;

            return $"{address}:{port}";
        }
    }
}