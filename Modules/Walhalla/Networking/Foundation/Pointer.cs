using System;
using Cutulu;

namespace Walhalla
{
    public class Pointer<T> where T : Target
    {
        // Unique user identification index
        public uint UUID { private set; get; }

        // Local delegates
        public Delegates.Empty onDisconnect;
        public Delegates.Packet onReceive;

        /// <summary> TRUE: ignore target receiving and thereby skip it </summary>
        public bool ignore_target_transfer;

        /// <summary> Custom params for the target class </summary>
        public object[] target_params;

        public Pointer(uint uuid, T target = null, Delegates.Packet receiver = null, Delegates.Empty disconnector = null)
        {
            this.target_params = new object[1] { this };
            this.ignore_target_transfer = false;
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
            if (target == null)
                return;

            T[] _ = new T[Targets.Length + 1];

            Array.Copy(Targets, _, Targets.Length);
            _[Targets.Length] = target;
        }

        /// <summary> Receive targets data </summary>
        protected virtual void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            // Iterate through all targets
            if (!ignore_target_transfer)
                lock (Targets)
                    for (int i = 0; i < Targets.Length; i++)
                    {
                        // Validate target
                        if (Targets[i] != null)
                        {
                            try
                            {
                                // Notify target
                                Targets[i].__receive(key, type, bytes, method, ignore_target_transfer);
                            }

                            catch (Exception ex)
                            {
                                $"[Pointer]: cannot receive packet because {ex.Message}".LogError();
                            }
                        }
                    }

            // Call delegates
            if (onReceive != null)
                lock (onReceive)
                {
                    onReceive(key, type, bytes, method);
                }
        }

        /// <summary> Notify targets about disconnect </summary>
        protected virtual void _disconnect()
        {
            // Iterate through all targets
            lock (Targets)
                for (int i = 0; i < Targets.Length; i++)
                {
                    // Validate target
                    if (Targets[i] != null)
                    {
                        // Notfiy target
                        Targets[i].__disconnect(ignore_target_transfer);
                    }
                }

            // Call delegates
            if (onDisconnect != null)
                lock (onDisconnect)
                {
                    onDisconnect();
                }
        }
    }
}