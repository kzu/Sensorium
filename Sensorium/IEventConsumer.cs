namespace Sensorium
{
    using System;
    using System.Reactive;

    public interface IEventConsumer : IDisposable
    {
        void Connect(IEventStream stream);
    }
}