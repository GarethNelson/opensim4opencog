using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace Simian
{
    /// <summary>
    /// Registers, unregisters, and fires events generated by incoming packets
    /// </summary>
    public class PacketEventDictionary
    {
        /// <summary>
        /// Object that is passed to worker threads in the ThreadPool for
        /// firing packet callbacks
        /// </summary>
        private struct PacketCallbackWrapper
        {
            /// <summary>Callback to fire for this packet</summary>
            public PacketCallback Callback;
            /// <summary>Reference to the agent that this packet came from</summary>
            public Agent Agent;
            /// <summary>The packet that needs to be processed</summary>
            public Packet Packet;
        }

        private Dictionary<PacketType, PacketCallback> _EventTable = new Dictionary<PacketType, PacketCallback>();
        private WaitCallback _ThreadPoolCallback;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PacketEventDictionary()
        {
            _ThreadPoolCallback = new WaitCallback(ThreadPoolDelegate);
        }

        /// <summary>
        /// Register an event handler
        /// </summary>
        /// <remarks>Use PacketType.Default to fire this event on every 
        /// incoming packet</remarks>
        /// <param name="packetType">Packet type to register the handler for</param>
        /// <param name="eventHandler">Callback to be fired</param>
        public void RegisterEvent(PacketType packetType, PacketCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(packetType))
                    _EventTable[packetType] += eventHandler;
                else
                    _EventTable[packetType] = eventHandler;
            }
        }

        /// <summary>
        /// Unregister an event handler
        /// </summary>
        /// <param name="packetType">Packet type to unregister the handler for</param>
        /// <param name="eventHandler">Callback to be unregistered</param>
        public void UnregisterEvent(PacketType packetType, PacketCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(packetType) && _EventTable[packetType] != null)
                    _EventTable[packetType] -= eventHandler;
            }
        }

        /// <summary>
        /// Fire the events registered for this packet type asynchronously
        /// </summary>
        /// <param name="packetType">Incoming packet type</param>
        /// <param name="packet">Incoming packet</param>
        /// <param name="agent">Agent this packet was received from</param>
        internal void BeginRaiseEvent(PacketType packetType, Packet packet, Agent agent)
        {
            PacketCallback callback;
            PacketCallbackWrapper wrapper;

            // Default handler first, if one exists
            if (_EventTable.TryGetValue(PacketType.Default, out callback))
            {
                if (callback != null)
                {
                    wrapper.Callback = callback;
                    wrapper.Packet = packet;
                    wrapper.Agent = agent;
                    ThreadPool.QueueUserWorkItem(_ThreadPoolCallback, wrapper);
                }
            }

            if (_EventTable.TryGetValue(packetType, out callback))
            {
                if (callback != null)
                {
                    wrapper.Callback = callback;
                    wrapper.Packet = packet;
                    wrapper.Agent = agent;
                    ThreadPool.QueueUserWorkItem(_ThreadPoolCallback, wrapper);

                    return;
                }
            }

            if (packetType != PacketType.Default && packetType != PacketType.PacketAck)
            {
                Logger.DebugLog("No handler registered for packet event " + packetType);
            }
        }

        private void ThreadPoolDelegate(Object state)
        {
            PacketCallbackWrapper wrapper = (PacketCallbackWrapper)state;

            try
            {
                wrapper.Callback(wrapper.Packet, wrapper.Agent);
            }
            catch (Exception ex)
            {
                Logger.Log("Async Packet Event Handler: " + ex.ToString(), Helpers.LogLevel.Error);
            }
        }
    }
}
