namespace Sensorium
{
    using System;
    using System.Reactive;

    public interface IDevice
    {
        string Id { get; }
        string Type { get; }

        IObservable<ISensed> Impulses { get; }
        void Send(IDo command);
    }
}