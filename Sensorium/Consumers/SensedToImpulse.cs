namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Text;

    /// <summary>
    /// Converts untyped 
    /// </summary>
    public class SensedToImpulse : IEventConsumer
    {
        private IClock clock;
        private IDictionary<string, TopicType> topicRegistry;
        private IDisposable subscription;

        public SensedToImpulse(IClock clock, IDictionary<string, TopicType> topicRegistry)
        {
            this.clock = clock;
            this.topicRegistry = topicRegistry;
        }

        public void Connect(IEventStream stream)
        {
            subscription = stream.Of<IEventPattern<IDevice, ISensed>>().Subscribe(sensed =>
            {
                var type = topicRegistry.Find(sensed.EventArgs.Topic);
                switch (type)
                {
                    case TopicType.Boolean:
                        stream.Push(sensed.Sender, Impulse.Create(sensed.EventArgs.Topic, Payload.ToBoolean(sensed.EventArgs.Payload), clock.Now));
                        break;
                    case TopicType.Number:
                        stream.Push(sensed.Sender, Impulse.Create(sensed.EventArgs.Topic, Payload.ToNumber(sensed.EventArgs.Payload), clock.Now));
                        break;
                    case TopicType.String:
                        stream.Push(sensed.Sender, Impulse.Create(sensed.EventArgs.Topic, Payload.ToString(sensed.EventArgs.Payload), clock.Now));
                        break;
                    case TopicType.Void:
                        stream.Push(sensed.Sender, Impulse.Create(sensed.EventArgs.Topic, Unit.Default, clock.Now));
                        break;
                    case TopicType.Unknown:
                    default:
                        // TODO: throw? Report?
                        break;
                }
            });
        }

        public void Dispose()
        {
            if (subscription != null)
                subscription.Dispose();
        }
    }
}