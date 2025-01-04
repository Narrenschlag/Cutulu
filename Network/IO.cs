namespace Cutulu.Network
{
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Net;

    using System.Linq;
    using Godot;

    using Core;
    using Web;

    using HttpRequest = Web.HttpRequest;

    public static class IO
    {
        public const string LocalhostIPv4 = "127.0.0.1";
        public const string LocalhostIPv6 = "::1";

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

        /// <summary>
        /// Returns full IPAddress in you local area network
        /// </summary>
        public static IPAddress GetLanIPv4()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip =>
                ip.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(ip));
        }

        /// <summary>
        /// Returns full IPAddress in you local area network
        /// </summary>
        public static IPAddress GetLanIPv6()
        {
            // Get all network interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface iface in interfaces)
            {
                // Filter out loopback and non-operational interfaces
                if (iface.NetworkInterfaceType != NetworkInterfaceType.Ethernet ||
                    iface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                // Get IPv6 addresses for the selected interface
                foreach (UnicastIPAddressInformation ip in iface.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 && !ip.Address.IsIPv6LinkLocal && !ip.Address.IsIPv6SiteLocal)
                    {
                        return ip.Address;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Opens a web request. If connected to the internet it will return your global IPAddress
        /// </summary>
        public static void GetGlobalIPv4(Node node, HttpRequest.Result result)
        {
            _ = new HttpRequest(node, "https://ipinfo.io/ip", result);
        }

        /// <summary>
        /// Opens a web request. If connected to the internet it will return your global IPAddress
        /// </summary>
        public static void GetGlobalIPv6(Node node, HttpRequest.Result result)
        {
            _ = new HttpRequest(node, "https://api6.ipify.org/", result);
        }
    }
}