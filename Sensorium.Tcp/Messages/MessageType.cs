namespace Sensorium
{
    using System;
    using System.Linq;

    /// <summary>
    /// Specifies the type of message being exchanged.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// A <see cref="Sensorium.Connect"/> message.
        /// </summary>
        Connect,
        /// <summary>
        /// A <see cref="Sensorium.Topic"/> message.
        /// </summary>
        Topic,
        /// <summary>
        /// A <see cref="Sensorium.Ping"/> message.
        /// </summary>
        Ping,
        /// <summary>
        /// A <see cref="Sensorium.Disconnect"/> message.
        /// </summary>
        Disconnect,
    }
}