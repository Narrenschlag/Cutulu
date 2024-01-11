using System;

namespace Cutulu
{
    public class Marker<D> where D : Destination
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

        public Marker(uint uuid, D destination = null, Protocol.Packet receiver = null, Protocol.Empty disconnector = null)
        {
            this.destination_params = new object[1] { this };
            this.ignore_detination_transfer = false;
            this.UUID = uuid;

            this.onDisconnect = disconnector;
            this.onReceive = receiver;

            SetTarget(destination);
        }

        // Target memory
        private D[] __destinations;

        /// <summary> Target array </summary>
        public D[] Destinations
        {
            set => __destinations = value;
            get
            {
                if (__destinations == null)
                {
                    __destinations = new D[0];
                }

                return __destinations;
            }
        }

        public void SetTarget(D destination)
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
                new D[1] { destination } :
                new D[0];

            // Notify addition to target
            if (destination != null)
                destination.__add(destination_params);
        }

        public void AddTarget(D destination)
        {
            if (destination == null)
                return;

            D[] _ = new D[Destinations.Length + 1];

            Array.Copy(Destinations, _, Destinations.Length);
            _[Destinations.Length] = destination;

            // Notify addition to target
            destination.__add(destination_params);
        }

        /// <summary> Receive targets data </summary>
        public virtual void _receive(byte key, byte[] bytes, Method method)
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
                                Destinations[i].__receive(key, bytes, method, destination_params);
                            }

                            catch (Exception ex)
                            {
                                if (Destinations.Length <= i || Destinations[i].IsNull())
                                {
                                    $"[Ignoring Marker]: Marker has been destroyed or is null".LogError();
                                }

                                else
                                {
                                    $"[Marker (class: {Destinations[i].GetType()})]: cannot receive packet because {ex.Message}\n{ex.StackTrace}".LogError();
                                }
                            }
                        }
                    }

            // Call delegates
            if (onReceive != null)
            {
                lock (onReceive)
                {
                    onReceive(key, bytes, method);
                }
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