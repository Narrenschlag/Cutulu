using System;

namespace Cutulu
{
    public class Marker<T> where T : Destination
    {
        // Unique user identification index
        public uint UUID { private set; get; }

        // Local delegates
        public Protocol.Empty onDisconnect;
        public Protocol.Packet onReceive;

        /// <summary> TRUE: ignore target receiving and thereby skip it </summary>
        public bool ignore_detination_transfer;

        /// <summary> Custom params for the target class </summary>
        public object[] destination_params;

        public Marker(uint uuid, T destination = null, Protocol.Packet receiver = null, Protocol.Empty disconnector = null)
        {
            this.destination_params = new object[1] { this };
            this.ignore_detination_transfer = false;
            this.UUID = uuid;

            this.onDisconnect = disconnector;
            this.onReceive = receiver;

            SetTarget(destination);
        }

        // Target memory
        private T[] __destinations;

        /// <summary> Target array </summary>
        public T[] Destinations
        {
            set => __destinations = value;
            get
            {
                if (__destinations == null)
                {
                    __destinations = new T[0];
                }

                return __destinations;
            }
        }

        public void SetTarget(T destination)
        {
            // Notify about left
            if (Destinations.NotEmpty())
            {
                lock (Destinations)
                    for (int i = 0; i < Destinations.Length; i++)
                    {
                        if (Destinations != null)
                        {
                            Destinations[i].__rem(destination_params);
                        }
                    }
            }

            // Assign array
            Destinations = destination != null ?
                new T[1] { destination } :
                new T[0];

            // Notify addition to target
            if (destination != null)
                destination.__add(destination_params);
        }

        public void AddTarget(T destination)
        {
            if (destination == null)
                return;

            T[] _ = new T[Destinations.Length + 1];

            Array.Copy(Destinations, _, Destinations.Length);
            _[Destinations.Length] = destination;

            // Notify addition to target
            destination.__add(destination_params);
        }

        /// <summary> Receive targets data </summary>
        protected virtual void _receive(byte key, BufferType type, byte[] bytes, Method method)
        {
            // Iterate through all targets
            if (!ignore_detination_transfer)
                lock (Destinations)
                    for (int i = 0; i < Destinations.Length; i++)
                    {
                        // Validate target
                        if (Destinations[i] != null)
                        {
                            try
                            {
                                // Notify target
                                Destinations[i].__receive(key, type, bytes, method, destination_params);
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
            lock (Destinations)
                for (int i = 0; i < Destinations.Length; i++)
                {
                    // Validate target
                    if (Destinations[i] != null)
                    {
                        // Notfiy target
                        Destinations[i].__disconnect(destination_params);
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