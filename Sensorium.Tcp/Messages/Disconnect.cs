namespace Sensorium
{
    using System;
    using System.Linq;

    /// <summary>
    /// Last message a disconnecting device should send to the server, 
    /// to allow clean disconnection instead of just waiting for 
    /// network timeout.
    /// </summary>
    public class Disconnect : IMessage
    {
        /// <summary>
        /// Type of message, which equals <see cref="MessageType.Disconnect"/>.
        /// </summary>
        public MessageType Type { get { return MessageType.Disconnect; } }
    }
}