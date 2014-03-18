namespace Sensorium
{
    using System;

    /// <summary>
    /// Basic interface for all exchanged messages.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Type of message being exchanged.
        /// </summary>
        MessageType Type { get; }
    }
}
