namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Regular data-carrying message that are used to 
    /// echange impulses or commands between the devices 
    /// and the server.
    /// </summary>
    public class Topic : IMessage
    {
        private string p;

        public Topic(string name, byte[] payload)
        {
            Name = name;
            Payload = payload;
        }

        public Topic(string name)
            : this(name, new byte[0])
        {
        }

        /// <summary>
        /// The message topic.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The optional payload exchanged.
        /// </summary>
        public byte[] Payload { get; private set; }

        /// <summary>
        /// Type of message, which equals <see cref="MessageType.Topic"/>.
        /// </summary>
        public MessageType Type { get { return MessageType.Topic; } }

        public override string ToString()
        {
            if (Payload.Length == 0)
                return Name;
            else if (Payload.Length == 1)
                return Name + " = " + BitConverter.ToBoolean(Payload, 0).ToString().ToLower();
            else
            {
                try
                {
                    return Name + " = " + BitConverter.ToSingle(Payload, 0) + "f";
                }
                catch
                {
                    return Name + " = \"" + Encoding.UTF8.GetString(Payload) + "\"";
                }
            }
        }
    }
}