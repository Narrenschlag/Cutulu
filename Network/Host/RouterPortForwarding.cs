namespace Cutulu.Network
{
    using Cutulu.Core;
    using Godot;

    /// <summary>
    /// <para>This class is used to open ports on the router for peer to peer networking</para>
    /// <para>Basically allowing you to skip the process of past methods and just open a local game others can remotely join</para>
    /// <para>Usage: Use <b>static OpenPortThread</b> or <b>local StartThread</b> to start opening the port</para>
    /// <para>Use <b>local Terminate</b> to close the port</para>
    /// </summary>
    public partial class RouterPortForwarding(int _port, RouterPortForwarding.PROTOCOL _protocol, string _desc = "", bool _useIPv6 = true) : GodotObject
    {
        /// <summary>
        /// Invoked when process of opening is done or has failed
        /// </summary>
        public event System.Action<RouterPortForwarding, Error> Completed;

        public GodotThread AsyncThread { get; private set; }
        public Upnp Upnp { get; private set; } = new();

        public PROTOCOL Protocol { get; private set; } = _protocol;
        public bool UseIPv6 { get; private set; } = _useIPv6;
        public string Desc { get; private set; } = _desc;
        public int Port { get; private set; } = _port;

        private Error Discover(int _timeout = 2000, int _ttl = 2, string _deviceFilter = "InternetGatewayService")
        {
            Upnp.DiscoverIpv6 = UseIPv6;
            return (Error)Upnp.Discover(_timeout, _ttl, _deviceFilter);
        }

        private Error Open()
        {
            var _gateway = Upnp.GetGateway();

            if (_gateway != null && _gateway.IsValidGateway())
            {
                return (Error)Upnp.AddPortMapping(Port, 0, Desc.IsEmpty() ? GetDefaultDescription() : Desc, GetProtocol());
            }

            return Error.Failed;
        }

        // Routine to open port on given data
        private void OpenPort()
        {
            var _error = Discover();

            if (_error == Error.Ok)
                _error = Open();

            Completed?.Invoke(this, _error);
        }

        /// <summary>
        /// Terminate thread and close port on router
        /// </summary>
        public void Terminate()
        {
            // Wait for the thread to finish
            AsyncThread?.WaitToFinish();
            AsyncThread = null;
        }

        /// <summary>
        /// Checks if thread is currently running
        /// </summary>
        public bool IsRunning()
        {
            return AsyncThread != null;
        }

        /// <summary>
        /// Starts thread to open port on router using given data
        /// </summary>
        public void StartThread()
        {
            // Terminate thread if already running
            Terminate();

            // Create and prepare new thread
            AsyncThread = new();
            var _call = new Callable(this, "OpenPort");

            // Start thread
            AsyncThread.Start(_call);
        }

        /// <summary>
        /// Starts thread to open port on router using given data
        /// </summary>
        public static RouterPortForwarding OpenPortThread(int _port, PROTOCOL _protocol, string _desc = "")
        {
            var _rpf = new RouterPortForwarding(_port, _protocol, _desc);

            _rpf.StartThread();

            return _rpf;
        }

        /// <summary>
        /// Returns true if any port can be opened
        /// </summary>
        public static bool CanOpenPort(out Error _error)
        {
            return (_error = new RouterPortForwarding(0, PROTOCOL.TCP).Discover()) == Error.Ok;
        }

        // Returns the name of the application
        private static string GetDefaultDescription()
        {
            return (string)ProjectSettings.GetSetting("application/config/name");
        }

        // Converts enum into string
        private string GetProtocol()
        {
            return Protocol switch
            {
                PROTOCOL.TCP => "TCP",
                _ => "UDP",
            };
        }

        /// <summary>
        /// Protocol used for port forwarding
        /// </summary>
        public enum PROTOCOL
        {
            UDP,
            TCP,
        }
    }
}