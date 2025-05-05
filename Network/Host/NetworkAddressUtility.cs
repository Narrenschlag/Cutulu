namespace Cutulu.Network
{
    using System.Net.NetworkInformation;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using System.Linq;
    using System.Net;
    using System;

    using Cutulu.Core;

    public static class NetworkAddressUtility
    {
        /// <summary>
        /// Gets a list of local IPv4 addresses and port that others can use to connect to your TCP listener
        /// </summary>
        /// <param name="port">The port your TCP listener is using</param>
        /// <returns>List of tuples containing (IPv4 address, port)</returns>
        public static List<string> GetLanIpv4Addresses(int port)
        {
            var _addressPairs = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Skip interfaces that are down, loopback, or not operational
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // Skip virtual interfaces and other special cases
                if (ni.Description.Contains("Virtual") || ni.Description.Contains("Pseudo"))
                    continue;

                // Get IP properties from the interface
                IPInterfaceProperties properties = ni.GetIPProperties();

                // Get IPv4 addresses
                foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    {
                        // Skip loopback addresses (127.x.x.x)
                        if (ip.Address.ToString().StartsWith("127."))
                            continue;

                        _addressPairs.Add(ip.Address.ToString());
                    }
                }
            }

            return _addressPairs;
        }

        /// <summary>
        /// Gets a list of local IPv6 addresses and port that others can use to connect to your TCP listener
        /// </summary>
        /// <param name="port">The port your TCP listener is using</param>
        /// <returns>List of tuples containing (IPv6 address, port)</returns>
        public static List<string> GetLanIpv6Addresses(this int port)
        {
            var _addressPairs = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Skip interfaces that are down, loopback, or not operational
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // Skip virtual interfaces and other special cases
                if (ni.Description.Contains("Virtual") || ni.Description.Contains("Pseudo"))
                    continue;

                // Get IP properties from the interface
                IPInterfaceProperties properties = ni.GetIPProperties();

                // Get IPv6 addresses
                foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
                    {
                        // Skip link-local addresses which start with fe80::
                        if (ip.Address.ToString().StartsWith("fe80:"))
                            continue;

                        _addressPairs.Add(ip.Address.ToString());
                    }
                }
            }

            return _addressPairs;
        }

        /// <summary>
        /// Gets the public IPv4 address and port that others outside your network can use to connect to your TCP listener
        /// </summary>
        /// <param name="port">The port your TCP listener is using</param>
        /// <returns>Tuple containing (WAN IPv4 address, port) or null if not available</returns>
        public static async Task<string> GetWanIpv4Address(this int port)
        {
            var _services = new List<string>
            {
                "https://api.ipify.org",
                "https://checkip.amazonaws.com",
                "https://icanhazip.com"
            };

            using (var _client = new System.Net.Http.HttpClient())
            {
                _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                _client.Timeout = TimeSpan.FromSeconds(5);

                foreach (var service in _services)
                {
                    try
                    {
                        var _external = await _client.GetStringAsync(service);
                        _external = _external.Trim();

                        if (IPAddress.TryParse(_external, out IPAddress ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ipAddress.ToString();
                        }
                    }
                    catch
                    {
                        // Try next service if this one fails
                        continue;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the public IPv6 address and port that others outside your network can use to connect to your TCP listener
        /// </summary>
        /// <param name="port">The port your TCP listener is using</param>
        /// <returns>Tuple containing (WAN IPv6 address, port) or null if not available</returns>
        public static async Task<string> GetWanIpv6Address(this int port)
        {
            var _services = new List<string>
            {
                "https://api6.ipify.org",
                "https://v6.ident.me"
            };

            using (var _handler = new System.Net.Http.SocketsHttpHandler())
            {
                // Configure handler to prefer IPv6 connections
                _handler.ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                };

                using (var _client = new System.Net.Http.HttpClient(_handler))
                {
                    _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                    _client.Timeout = TimeSpan.FromSeconds(5);

                    foreach (var service in _services)
                    {
                        try
                        {
                            var _external = await _client.GetStringAsync(service);
                            _external = _external.Trim();

                            if (IPAddress.TryParse(_external, out IPAddress ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                return ipAddress.ToString();
                            }
                        }
                        catch
                        {
                            // Try next service if this one fails
                            continue;
                        }
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Example showing how to use these functions and format the addresses for display
        /// </summary>
        public static async Task DisplayAddressesExample()
        {
            int _port = 8080;

            Debug.Log("=== TCP SERVER CONNECTION INFORMATION ===");
            Debug.Log($"Server running on port: {_port}");

            // Get and display LAN IPv4 addresses
            Debug.Log("\nLocal Network (LAN) IPv4 Addresses:");
            var ipv4Addresses = GetLanIpv4Addresses(_port);
            if (ipv4Addresses.Any())
            {
                foreach (var _address in ipv4Addresses)
                {
                    Debug.Log($"  {_address}, Port: {_port}");
                    // Or format however you need, e.g.: Console.WriteLine($"  {address}:{port}");
                }
            }
            else
            {
                Debug.Log("  No IPv4 addresses available.");
            }

            // Get and display LAN IPv6 addresses
            Debug.Log("\nLocal Network (LAN) IPv6 Addresses:");
            var ipv6Addresses = GetLanIpv6Addresses(_port);
            if (ipv6Addresses.Any())
            {
                foreach (var _address in ipv6Addresses)
                {
                    Debug.Log($"  {_address}, Port: {_port}");
                    // For IPv6 in URLs: Console.WriteLine($"  [{address}]:{port}");
                }
            }
            else
            {
                Debug.Log("  No IPv6 addresses available.");
            }

            // Get and display WAN IPv4 address
            Debug.Log("\nInternet (WAN) IPv4 Address:");
            var wanIpv4 = await GetWanIpv4Address(_port);
            if (wanIpv4.NotEmpty())
            {
                Debug.Log($"  {wanIpv4}, Port: {_port}");
            }
            else
            {
                Debug.Log("  No WAN IPv4 address available.");
            }

            // Get and display WAN IPv6 address
            Debug.Log("\nInternet (WAN) IPv6 Address:");
            var wanIpv6 = await GetWanIpv6Address(_port);
            if (wanIpv6.NotEmpty())
            {
                Debug.Log($"  {wanIpv6}, Port: {_port}");
            }
            else
            {
                Debug.Log("  No WAN IPv6 address available.");
            }

            Debug.Log("\nNote: For WAN connections to work, you must set up port forwarding on your router.");
        }
    }
}