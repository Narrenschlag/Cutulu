using System;

namespace Cutulu
{
    public class Marker<D> where D : Destination
    {
        // Unique user identification index
        public uint UUID { private set; get; }

        // Safety Identification for unknown udp packages
        public ushort SafetyId { protected set; get; }

        // Local delegates
        public Protocol.Empty onDisconnect;
        public Protocol.Packet onReceive;

        /// <summary> TRUE: ignore target receiving and thereby skip it </summary>
        public bool ignore_detination_transfer;

        /// <summary> Custom params for the target class </summary>
        public object[] destination_params;

        public Marker(uint uuid, ushort safetyId, D destination = null, Protocol.Packet receiver = null, Protocol.Empty disconnector = null)
        {
            this.destination_params = new object[1] { this };
            this.ignore_detination_transfer = false;

            this.SafetyId = safetyId;
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
                __destinations ??= Array.Empty<D>();

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
                        Destinations?[i].Rem(destination_params);
                    }
            }

            // Assign array
            Destinations = destination != null ?
                new D[1] { destination } :
                Array.Empty<D>();

            // Notify addition to target
            destination?.Add(destination_params);
        }

        public void AddTarget(D destination)
        {
            if (destination == null)
                return;

            D[] _ = new D[Destinations.Length + 1];

            Array.Copy(Destinations, _, Destinations.Length);
            _[Destinations.Length] = destination;

            // Notify addition to target
            destination.Add(destination_params);
        }

        /// <summary> Receive targets data </summary>
        public virtual void Receive(byte key, byte[] bytes, Method method)
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
                                Destinations[i].Receive(key, bytes, method, destination_params);
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
        protected virtual void Disconnected()
        {
            // Iterate through all targets
            lock (Destinations)
                for (int i = 0; i < Destinations.Length; i++)
                {
                    // Validate target
                    // Notfiy target
                    Destinations[i]?.Disconnect(destination_params);
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