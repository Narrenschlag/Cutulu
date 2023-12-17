using System;

namespace Walhalla
{
    public class Pointer<T> where T : Target
    {
        // Unique user identification index
        public uint UUID;

        // Local delegates
        public Delegates.Empty onDisconnect;
        public Delegates.Packet onReceive;

        public Pointer(uint uuid, T target = null, Delegates.Packet receiver = null, Delegates.Empty disconnector = null)
        {
            this.UUID = uuid;

            this.onDisconnect = disconnector;
            this.onReceive = receiver;

            SetTarget(target);
        }

        // Target memory
        private T[] __targets;

        /// <summary> Target array </summary>
        public T[] Targets
        {
            set => __targets = value;
            get
            {
                if (__targets == null)
                {
                    __targets = new T[0];
                }

                return __targets;
            }
        }

        public void SetTarget(T target)
        {
            Targets = target != null ?
                new T[1] { target } :
                new T[0];
        }

        public void AddTarget(T target)
        {
            T[] _ = new T[Targets.Length + 1];

            Array.Copy(Targets, _, Targets.Length);
            _[Targets.Length] = target;
        }

        /// <summary> Receive targets data </summary>
        protected virtual void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            // Iterate through all targets
            for (int i = 0; i < Targets.Length; i++)
            {
                // Valdiate target
                if (Targets[i] != null)
                {
                    // Notfiy target
                    Targets[i].__receive(key, type, bytes, method, this);
                }
            }

            // Call delegates
            if (onReceive != null)
            {
                onReceive(key, type, bytes, method);
            }
        }

        /// <summary> Notify targets about disconnect </summary>
        protected virtual void _disconnect()
        {
            // Iterate through all targets
            for (int i = 0; i < Targets.Length; i++)
            {
                // Valdiate target
                if (Targets[i] != null)
                {
                    // Notfiy target
                    Targets[i].__disconnect(this);
                }
            }

            // Call delegates
            if (onDisconnect != null)
            {
                onDisconnect();
            }
        }
    }
}