namespace Sensorium
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    // TODO: this class can be vastly improved for in-memory performance.
    public class SystemState : ISystemState
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<string, object>> state = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();

        public void Set<T>(string topic, string device, T value)
        {
            Guard.NotNullOrEmpty(() => topic, topic);
            Guard.NotNullOrEmpty(() => device, device);

            var topicState = state.GetOrAdd(topic,
                _ => new ConcurrentDictionary<string, object>());

            topicState[device] = value;
        }

        public bool Equals<T>(string topic, string device, T value)
        {
            Guard.NotNullOrEmpty(() => topic, topic);
            Guard.NotNullOrEmpty(() => device, device);

            var topicState = state.GetOrAdd(topic,
                _ => new ConcurrentDictionary<string, object>());

            object current;
            return topicState.TryGetValue(device, out current) && Object.Equals(current, value);
        }

        public IQueryable<T> Of<T>(string topic, string optionalDeviceIds)
        {
            Guard.NotNullOrEmpty(() => topic, topic);

            if (string.IsNullOrEmpty(optionalDeviceIds))
                return state.AsQueryable()
                    .Where(topicState => topicState.Key == topic)
                    .SelectMany(topicState => topicState.Value.Select(deviceState => deviceState.Value))
                    .OfType<T>();

            var ids = new HashSet<string>(optionalDeviceIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id)));

            return state.AsQueryable()
                .Where(topicState => topicState.Key == topic)
                .SelectMany(topicState => topicState.Value)
                .Where(deviceState => ids.Contains(deviceState.Key))
                .Select(deviceState => deviceState.Value)
                .OfType<T>();
        }
    }
}