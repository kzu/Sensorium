namespace Sensorium
{
    using System;
    using System.Reactive;

    public static class Impulse
    {
        public static IImpulse<T> Create<T>(string topic, T payload, DateTimeOffset timestamp)
        {
            return new ImpulseImpl<T>(topic, payload, timestamp);
        }

        public static IImpulse<Unit> Create(string topic, DateTimeOffset timestamp)
        {
            return new ImpulseImpl<Unit>(topic, Unit.Default, timestamp);
        }

        internal class ImpulseImpl<T> : IImpulse<T>
        {
            public ImpulseImpl(string topic, T payload, DateTimeOffset timestamp)
            {
                this.Timestamp = timestamp;
                this.Topic = topic;
                this.Payload = payload;
            }

            public DateTimeOffset Timestamp { get; private set; }
            public string Topic { get; private set; }
            public T Payload { get; private set; }

            public override bool Equals(object obj)
            {
                var other = obj as ImpulseImpl<T>;
                if (other == null)
                    return false;

                return this.Topic == other.Topic &&
                    this.Timestamp == other.Timestamp &&
                    this.Payload.Equals(other.Payload);
            }

            public override int GetHashCode()
            {
                return this.Topic.GetHashCode() ^ this.Timestamp.GetHashCode() ^ this.Payload.GetHashCode();
            }

            public override string ToString()
            {
                return "Impulse<" + typeof(T).Name + ">('" + this.Topic + "', " +
                    (typeof(T) == typeof(Unit) ? "" : this.Payload.ToString()) + "')";
            }
        }
    }
}