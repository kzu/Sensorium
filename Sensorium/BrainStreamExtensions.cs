namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    public static class BrainStreamExtensions
    {
        public static IObservable<T> Impulses<T>(this IEventStream stream, string topic, string optionalDeviceIds)
        {
            if (string.IsNullOrEmpty(optionalDeviceIds))
                return stream.Of<IImpulse<T>>().Where(x => x.Topic == topic).Select(x => x.Payload);

            var ids = new HashSet<string>(optionalDeviceIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id)));

            return stream.Of<IEventPattern<IDevice, IImpulse<T>>>()
                .Where(x => x.EventArgs.Topic == topic && ids.Contains(x.Sender.Id))
                .Select(x => x.EventArgs.Payload);
        }

        public static IObservable<T> Commands<T>(this IEventStream stream, string topic, string optionalDeviceIds)
        {
            if (string.IsNullOrEmpty(optionalDeviceIds))
                return stream.Of<ICommand<T>>().Where(x => x.Topic == topic).Select(x => x.Payload);

            return stream.Of<ICommand<T>>()
                         .Where(x => x.Topic == topic && x.TargetsDevice(optionalDeviceIds))
                         .Select(x => x.Payload);
        }
    }
}