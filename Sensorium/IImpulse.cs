namespace Sensorium
{
    using System;

    public interface IImpulse
    {
        string Topic { get; }
        DateTimeOffset Timestamp { get; }
    }

    public interface IImpulse<out T> : IImpulse
    {
        T Payload { get; }
    }
}