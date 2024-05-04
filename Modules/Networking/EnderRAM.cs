using System.Collections.Generic;

namespace Cutulu
{
    public class EnderRAM<Receiver> where Receiver : Cutulu.Receiver
    {
        public short EnderKey { get; private set; }

        private Dictionary<short, object> RAM => _ram ??= new();
        private Dictionary<short, object> _ram;

        private ServerConnection<Receiver> Connection;
        private ClientNetwork<Receiver> Client;
        private ServerNetwork<Receiver> Server;

        public EnderRAM(ServerNetwork<Receiver> tcp, short uniquePackageKey)
        {
            EnderKey = uniquePackageKey;
            Server = tcp;
        }

        public EnderRAM(ClientNetwork<Receiver> tcp, short uniquePackageKey)
        {
            EnderKey = uniquePackageKey;
            Client = tcp;
        }

        public EnderRAM(ServerConnection<Receiver> tcp, short uniquePackageKey)
        {
            EnderKey = uniquePackageKey;
            Connection = tcp;
        }

        public void Clear() => RAM.Clear();

        public void Set<T>(short key, T value)
        {
            if (RAM.TryGetValue(key, out var existing))
            {
                if (existing.Equals(value) == false) RAM[key] = value;
                else return;
            }

            else RAM.Add(key, value);

            Server?.Broadcast(EnderKey, value, Method.Tcp);
            Connection?.Send(EnderKey, value, Method.Tcp);
            Client?.Send(EnderKey, value, Method.Tcp);
        }

        public bool TryGet<T>(short key, out T value)
        {
            if (RAM.TryGetValue(key, out var obj) && obj is T v)
            {
                value = v;
                return true;
            }

            value = default;
            return false;
        }

        public void SyncAll()
        {
            foreach (var pair in RAM)
            {
                Set(pair.Key, pair.Value);
            }
        }
    }
}