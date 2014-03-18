namespace Sensorium
{
    using System;
    using System.Linq;

    /// <summary>
    /// An empty message to let the server know the device is still 
    /// alive, used when no other messages are exchanged for the time 
    /// being.
    /// </summary>
    public class Ping : IMessage
    {
        /// <summary>
        /// Type of message, which equals <see cref="MessageType.Ping"/>.
        /// </summary>
        public MessageType Type { get { return MessageType.Ping; } }
    }
}