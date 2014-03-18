namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;

    public class ClockImpulses : IEventConsumer
    {
        private IDisposable subscription;
        private IClock clock;

        public ClockImpulses(IClock clock)
        {
            this.clock = clock;
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }

        public void Connect(IEventStream stream)
        {
            subscription = clock.Tick.Subscribe(tick =>
            {
                stream.Push(Impulse.Create(Topics.System.Date, tick, clock.Now));
                stream.Push(Impulse.Create(Topics.System.Day, tick.Day, clock.Now));
                stream.Push(Impulse.Create(Topics.System.Month, tick.Month, clock.Now));
                stream.Push(Impulse.Create(Topics.System.Year, tick.Year, clock.Now));

                stream.Push(Impulse.Create(Topics.System.Time, new TimeSpan(tick.Hour, tick.Minute, tick.Second), clock.Now));
                stream.Push(Impulse.Create(Topics.System.Hour, tick.Hour, clock.Now));
                stream.Push(Impulse.Create(Topics.System.Minute, tick.Minute, clock.Now));
                stream.Push(Impulse.Create(Topics.System.Second, tick.Second, clock.Now));
            });
        }
    }
}