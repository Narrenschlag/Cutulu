using System;

namespace Cutulu
{
    /// <summary> 
    /// A Marker contains the base for Server and Client connentions
    /// </summary>
    public class Marker<R> where R : Receiver
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
        public bool ignore_receiver_transfer;

        /// <summary>
        /// Custom params for the target class
        /// </summary>
        public object[] receiver_params;

        #region Setup               ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Marker(uint uuid, ushort safetyId, R receiver = null, Protocol.Packet onReceive = null, Protocol.Empty disconnector = null)
        {
            receiver_params = new object[1] { this };
            ignore_receiver_transfer = false;

            SafetyId = safetyId;
            UUID = uuid;

            onDisconnect = disconnector;
            this.onReceive = onReceive;

            SetReceiver(receiver);
        }
        #endregion

        #region Receivers        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Receiver memory
        private R[] __receivers, __subReceivers;

        /// <summary>
        /// Collection of Receivers that receive the incomming traffic
        /// </summary>
        public R[] Receivers
        {
            set => __receivers = value;
            get
            {
                __receivers ??= Array.Empty<R>();

                return __receivers;
            }
        }

        /// <summary>
        /// Collection of sub Receivers that receive the incomming traffic
        /// </summary>
        public R[] SubReceivers
        {
            set => __subReceivers = value;
            get
            {
                __subReceivers ??= Array.Empty<R>();

                return __subReceivers;
            }
        }

        /// <summary>
        /// Sets target Receivers for all incomming packages
        /// </summary>
        public void SetReceiver(params R[] receivers)
        {
            // Notify about left
            if (Receivers.NotEmpty())
            {
                lock (Receivers)
                {
                    for (int i = 0; i < Receivers.Length; i++)
                    {
                        Receivers?[i].Rem(receiver_params);
                    }
                }
            }

            // Assign array
            Receivers = receivers;

            // Notify addition to target
            if (receivers != null)
            {
                for (int i = 0; i < receivers.Length; i++)
                {
                    receivers[i]?.Add(receiver_params);
                }
            }
        }

        /// <summary>
        /// Sets target receiver for all incomming packages
        /// </summary>
        public void SetSubReceiver(params R[] receivers)
        {
            // Notify about left
            if (SubReceivers.NotEmpty())
            {
                lock (SubReceivers)
                {
                    for (int i = 0; i < SubReceivers.Length; i++)
                    {
                        SubReceivers?[i].Rem(receiver_params);
                    }
                }
            }

            // Assign array
            SubReceivers = receivers;

            // Notify addition to target
            if (receivers != null)
            {
                for (int i = 0; i < receivers.Length; i++)
                {
                    receivers[i]?.Add(receiver_params);
                }
            }
        }
        #endregion

        #region Async Events        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Distributes all the incomming traffic to all registered receivers
        /// </summary>
        public virtual void Receive(ref NetworkPackage package)
        {
            // Iterate through all targets
            if (!ignore_receiver_transfer)
            {
                receiver(Receivers, ref package);
                receiver(SubReceivers, ref package);
            }

            // Call delegates
            if (onReceive != null)
            {
                lock (onReceive)
                {
                    onReceive?.Invoke(ref package);
                }
            }

            // Iterate through receivers and call their receive functions
            void receiver(R[] array, ref NetworkPackage package)
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
                                array[i].Receive(ref package, receiver_params);
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
            lock (Receivers)
            {
                for (int i = 0; i < Receivers.Length; i++)
                {
                    // Validate target
                    // Notfiy target
                    Receivers[i]?.Disconnect(receiver_params);
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