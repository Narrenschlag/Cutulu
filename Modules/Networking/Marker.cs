using System;

namespace Cutulu
{
    /// <summary> 
    /// A Marker contains the base for Server and Client connentions
    /// </summary>
    public class Marker<D> where D : Destination
    {
        // Unique user identification index
        public uint UUID { private set; get; }

        // Safety Identification for unknown udp packages
        public ushort SafetyId { protected set; get; }

        // Local delegates
        public Protocol.Empty onDisconnect;
        public Protocol.Packet onReceive;

        /// <summary> 
        /// TRUE: ignore target receiving and thereby skip it 
        /// </summary>
        public bool ignore_destination_transfer;

        /// <summary>
        /// Custom params for the target class
        /// </summary>
        public object[] destination_params;

        #region Setup               ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Marker(uint uuid, ushort safetyId, D destination = null, Protocol.Packet receiver = null, Protocol.Empty disconnector = null)
        {
            destination_params = new object[1] { this };
            ignore_destination_transfer = false;

            SafetyId = safetyId;
            UUID = uuid;

            onDisconnect = disconnector;
            onReceive = receiver;

            SetDestination(destination);
        }
        #endregion

        #region Destinations        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Destination memory
        private D[] __destinations, __subDestinations;

        /// <summary>
        /// Collection of Destinations that receive the incomming traffic
        /// </summary>
        public D[] Destinations
        {
            set => __destinations = value;
            get
            {
                __destinations ??= Array.Empty<D>();

                return __destinations;
            }
        }

        /// <summary>
        /// Collection of sub Destinations that receive the incomming traffic
        /// </summary>
        public D[] SubDestinations
        {
            set => __subDestinations = value;
            get
            {
                __subDestinations ??= Array.Empty<D>();

                return __subDestinations;
            }
        }

        /// <summary>
        /// Sets target destination for all incomming packages
        /// </summary>
        public void SetDestination(params D[] destinations)
        {
            // Notify about left
            if (Destinations.NotEmpty())
            {
                lock (Destinations)
                {
                    for (int i = 0; i < Destinations.Length; i++)
                    {
                        Destinations?[i].Rem(destination_params);
                    }
                }
            }

            // Assign array
            Destinations = destinations;

            // Notify addition to target
            if (destinations != null)
            {
                for (int i = 0; i < destinations.Length; i++)
                {
                    destinations[i]?.Add(destination_params);
                }
            }
        }

        /// <summary>
        /// Sets target destination for all incomming packages
        /// </summary>
        public void SetSubDestination(params D[] destinations)
        {
            // Notify about left
            if (SubDestinations.NotEmpty())
            {
                lock (SubDestinations)
                {
                    for (int i = 0; i < SubDestinations.Length; i++)
                    {
                        SubDestinations?[i].Rem(destination_params);
                    }
                }
            }

            // Assign array
            SubDestinations = destinations;

            // Notify addition to target
            if (destinations != null)
            {
                for (int i = 0; i < destinations.Length; i++)
                {
                    destinations[i]?.Add(destination_params);
                }
            }
        }
        #endregion

        #region Async Events        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Distributes all the incomming traffic to all registered destinations
        /// </summary>
        public virtual void Receive(byte key, byte[] bytes, Method method)
        {
            // Iterate through all targets
            if (!ignore_destination_transfer)
            {
                dest(Destinations);
                dest(SubDestinations);
            }

            // Call delegates
            if (onReceive != null)
            {
                lock (onReceive)
                {
                    onReceive(key, bytes, method);
                }
            }

            // Iterate through destinations and call their receive functions
            void dest(D[] array)
            {
                if (array == null || array.Length < 1) return;

                lock (array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        // Validate target
                        if (array[i] != null)
                        {
                            // Success
                            try
                            {
                                // Notify target
                                array[i].Receive(key, bytes, method, destination_params);
                            }

                            // Failed
                            catch (Exception ex)
                            {
                                if (array.Length <= i || array[i].IsNull())
                                {
                                    $"[Ignoring Marker]: Marker has been destroyed or is null".LogError();
                                }

                                else
                                {
                                    $"[Marker (class: {array[i].GetType()})]: cannot receive packet because {ex.Message}\n{ex.StackTrace}".LogError();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary> 
        /// Notify targets about disconnect 
        /// </summary>
        protected virtual void Disconnected()
        {
            // Iterate through all targets
            lock (Destinations)
            {
                for (int i = 0; i < Destinations.Length; i++)
                {
                    // Validate target
                    // Notfiy target
                    Destinations[i]?.Disconnect(destination_params);
                }
            }

            // Call delegates
            if (onDisconnect != null)
            {
                lock (onDisconnect)
                {
                    onDisconnect();
                }
            }
        }
        #endregion
    }
}