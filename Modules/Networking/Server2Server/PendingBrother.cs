using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System;
using System.Net;

namespace Cutulu
{
    public class PendingBrother
    {
        public const int VerificationTimeWindow = 4;

        public readonly NetworkFamily Family;
        public readonly TcpProtocol Remote;

        private readonly Dictionary<string, Passkey> RemoteKeys;
        private readonly Action<NetworkBrother> Callback;

        private bool Verified;

        public PendingBrother(TcpClient client, NetworkFamily family, Dictionary<string, Passkey> remoteKeys, Action<NetworkBrother> callback)
        {
            Callback = callback;
            Family = family;

            RemoteKeys = remoteKeys;
            Verified = false;

            Remote = new(ref client, 0, OnReceive, Cancel, 0);
        }

        private void OnReceive(byte key, byte[] bytes, Method method)
        {
            switch (key)
            {
                // Verify
                case 120:
                    if (bytes.TryBuffer(out PendingRequest request) && Family.CompareKey(request.Key))
                    {
                        if (RemoteKeys.ContainsKey(request.Id))
                        {
                            var brother = new NetworkBrother(this, request.Id, request.Port);
                            Callback?.Invoke(brother);
                            return;
                        }

                        else Debug.LogError($"Brother {request.Id} is not welcome.");
                    }

                    break;

                default: break;
            }

            Cancel();
        }

        #region Time Window
        /// <summary>
        /// Starts time window in which the verification process has to be completed. Else close connection.
        /// </summary>
        private async void StartTimeWindow()
        {
            await Task.Delay(1000 * VerificationTimeWindow);

            if (Verified == false) Cancel();
        }

        private void Cancel()
        {
            Remote?.Close();
        }
        #endregion

        public struct PendingRequest
        {
            public Passkey Key { get; set; }
            public string Id { get; set; }
            public int Port { get; set; }

            public PendingRequest(NetworkFamily family, ref Passkey remoteKey, string remoteId)
            {
                Port = ((IPEndPoint)family.Listener.LocalEndpoint).Port;

                Key = remoteKey;
                Id = remoteId;
            }
        }
    }
}