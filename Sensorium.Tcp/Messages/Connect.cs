namespace Sensorium
{
    using System;
    using System.Linq;
    
    /// <summary>
    /// First message that a client must send after establishing 
    /// a TCP connection, which tells the server the identifier 
    /// and type of client device.
    /// </summary>
    public class Connect : IMessage
    {
        public Connect(string deviceId, string deviceType)
        {
            DeviceId = deviceId;
            DeviceType = deviceType;
        }

        /// <summary>
        /// Identifier for the connecting device.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Type of device, which should be registered 
        /// by the vendor previously.
        /// </summary>
        public string DeviceType { get; private set; }

        /// <summary>
        /// Type of message, which equals <see cref="MessageType.Connect"/>.
        /// </summary>
        public MessageType Type { get { return MessageType.Connect; } }
    }
}