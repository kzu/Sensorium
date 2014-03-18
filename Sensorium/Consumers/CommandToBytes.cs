namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Text;

    /// <summary>
    /// Converts outgoing typed commands to their byte array versions.
    /// </summary>
    public class CommandToBytes : IEventConsumer
    {
        private IDisposable subscription;

        public void Connect(IEventStream stream)
        {
            subscription = new CompositeDisposable(
                stream.Of<ICommand<bool>>().Subscribe(cmd => 
                    stream.Push(Command.Create(cmd.Topic, Payload.ToBytes(cmd.Payload), cmd.Timestamp, cmd.TargetDeviceIds))),
                stream.Of<ICommand<float>>().Subscribe(cmd =>
                    stream.Push(Command.Create(cmd.Topic, Payload.ToBytes(cmd.Payload), cmd.Timestamp, cmd.TargetDeviceIds))),
                stream.Of<ICommand<string>>().Subscribe(cmd =>
                    stream.Push(Command.Create(cmd.Topic, Payload.ToBytes(cmd.Payload), cmd.Timestamp, cmd.TargetDeviceIds))),
                stream.Of<ICommand<Unit>>().Subscribe(cmd =>
                    stream.Push(Command.Create(cmd.Topic, new byte[0], cmd.Timestamp, cmd.TargetDeviceIds))));
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }
    }
}