namespace Sensorium
{
    using System;
    using System.Linq;

    public class Do : IDo
    {
        public Do(string topic, byte[] payload)
        {
            this.Topic = topic;
            this.Payload = payload;
        }

        public string Topic { get; private set; }
        public byte[] Payload { get; private set; }
    }
}