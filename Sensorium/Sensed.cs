namespace Sensorium
{
    using System;
    using System.Linq;

    public class Sensed : ISensed
    {
        public Sensed(string topic, byte[] payload)
        {
            this.Topic = topic;
            this.Payload = payload;
        }

        public string Topic { get; private set; }
        public byte[] Payload { get; private set; }

        public override string ToString()
        {
            return "Sensed('" + Topic + "', bytes[" + Payload.Length + "])";
        }
    }
}