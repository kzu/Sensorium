namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;

    public class StoreImpulses : IEventConsumer
    {
        private IImpulseStore store;
        private IDisposable subscription;

        public StoreImpulses(IImpulseStore store)
        {
            this.store = store;
        }

        public void Connect(IEventStream stream)
        {
            subscription = stream.Of<IDevice, IImpulse>().Subscribe(e => store.Save(e.Sender, e.EventArgs));
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }
    }
}