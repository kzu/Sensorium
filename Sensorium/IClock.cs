namespace Sensorium
{
    using System;

    public interface IClock
    {
        DateTimeOffset Now { get; }
        IObservable<DateTimeOffset> Tick { get; }
    }
}