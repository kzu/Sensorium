namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;

    public class SetSystemState : IEventConsumer
    {
        private ISystemState state;
        private IDisposable subscription;

        public SetSystemState(ISystemState state)
        {
            this.state = state;
        }

        public void Connect(IEventStream stream)
        {
            subscription = new CompositeDisposable(
                stream.Of<IEventPattern<IDevice, IImpulse<bool>>>()
                      .Subscribe(i => state.Set(i.EventArgs.Topic, i.Sender.Id, i.EventArgs.Payload)), 
                stream.Of<IEventPattern<IDevice, IImpulse<float>>>()
                      .Subscribe(i => state.Set(i.EventArgs.Topic, i.Sender.Id, i.EventArgs.Payload)),
                stream.Of<IEventPattern<IDevice, IImpulse<string>>>()
                      .Subscribe(i => state.Set(i.EventArgs.Topic, i.Sender.Id, i.EventArgs.Payload)));
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }
    }
}