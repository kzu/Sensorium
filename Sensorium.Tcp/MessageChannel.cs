namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Higher-level channel that translates the inner channel bytes 
    /// into typed messages.
    /// </summary>
    public class MessageChannel : IChannel<IMessage>
    {
        private BinaryChannel innerChannel;

        /// <summary>
        /// Initializes the channel with the wrapped binary channel.
        /// </summary>
        public MessageChannel(BinaryChannel innerChannel)
        {
            this.innerChannel = innerChannel;

            this.Receiver = innerChannel.Receiver
                .Select(bytes => Convert(bytes));
        }

        /// <summary>
        /// Stream of received messages.
        /// </summary>
        public IObservable<IMessage> Receiver { get; private set; }

        /// <summary>
        /// Asynchronously sends a message through the channel.
        /// </summary>
        public Task SendAsync(IMessage message)
        {
            return innerChannel.SendAsync(Convert(message));
        }

        /// <summary>
        /// Converts a binary payload to the typed message represented by 
        /// the first byte of the array.
        /// </summary>
        internal static IMessage Convert(byte[] payload)
        {
            var type = (MessageType)payload[0];
            switch (type)
            {
                case MessageType.Connect:
                    var idSize = payload[1];
                    var deviceId = Encoding.UTF8.GetString(payload, 2, idSize);
                    var typeSize = payload[idSize + 2];
                    var deviceType = Encoding.UTF8.GetString(payload, idSize + 3, typeSize);
                    return new Connect(deviceId, deviceType);
                case MessageType.Topic:
                    var topicSize = payload[1];
                    var topic = Encoding.UTF8.GetString(payload, 2, topicSize);
                    return new Topic(topic, payload.Skip(2 + topicSize).ToArray());
                case MessageType.Ping:
                    return new Ping();
                case MessageType.Disconnect:
                    return new Disconnect();
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

        /// <summary>
        /// Converts the message to a binary array that is prefixed 
        /// with a byte specifying the length of the array.
        /// </summary>
        internal static byte[] Convert(IMessage payload)
        {
            var bytes = new List<byte>();
            bytes.Add((byte)payload.Type);
            switch (payload.Type)
            {
                case MessageType.Connect:
                    var connect = (Connect)payload;
                    var id = Encoding.UTF8.GetBytes(connect.DeviceId);
                    var type = Encoding.UTF8.GetBytes(connect.DeviceType);
                    if ((id.Length + type.Length + 3) > byte.MaxValue)
                        throw new ArgumentOutOfRangeException("Total length of the serialized message is larger than the maximum " + byte.MaxValue);

                    bytes.Add((byte)id.Length);
                    bytes.AddRange(id);
                    bytes.Add((byte)type.Length);
                    bytes.AddRange(type);
                    break;
                case MessageType.Topic:
                    var topic = (Topic)payload;
                    var name = Encoding.UTF8.GetBytes(topic.Name);
                    if ((name.Length + topic.Payload.Length + 2) > byte.MaxValue)
                        throw new ArgumentOutOfRangeException("Total length of the serialized message is larger than the maximum " + byte.MaxValue);

                    bytes.Add((byte)name.Length);
                    bytes.AddRange(name);
                    bytes.AddRange(topic.Payload);
                    break;
                case MessageType.Ping:
                case MessageType.Disconnect:
                default:
                    break;
            }

            return bytes.ToArray();
        }
    }
}