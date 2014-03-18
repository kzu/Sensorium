namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;

    public class StoreCommands : IEventConsumer
    {
        private ICommandStore store;
        private IDisposable subscription;

        public StoreCommands(ICommandStore store)
        {
            this.store = store;
        }

        public void Connect(IEventStream stream)
        {
            subscription = stream.Of<IDevice, IssuedCommand>().Subscribe(e => store.Save(e.Sender, e.EventArgs));
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }
    }
}