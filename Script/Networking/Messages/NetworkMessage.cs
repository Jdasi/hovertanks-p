using System;

namespace HoverTanks.Networking
{
    public abstract class NetworkMessage : IDisposable
    {
        /// <summary> Unique identifier of the message type. Not part of the message data. </summary>
        public abstract MessageId _MessageId { get; }

        public virtual void Write(Packet packet) { }
        public virtual void Read(Packet packet) { }

        public void Dispose() { }
    }
}
