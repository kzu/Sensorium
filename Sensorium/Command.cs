namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;

    public static class Command
    {
        public static ICommand<T> Create<T>(string topic, T payload, DateTimeOffset timestamp, string optionalTargetDeviceIds = "")
        {
            return new CommandImpl<T>(topic, payload, optionalTargetDeviceIds ?? "", timestamp);
        }

        public static ICommand<Unit> Create(string topic, DateTimeOffset timestamp, string optionalTargetDeviceIds = "")
        {
            return new CommandImpl<Unit>(topic, Unit.Default, optionalTargetDeviceIds ?? "", timestamp);
        }

        internal class CommandImpl<T> : ICommand<T>
        {
            private HashSet<string> ids;
            private Func<string, bool> targets;

            public CommandImpl(string topic, T payload, string deviceIds, DateTimeOffset timestamp)
            {
                this.Timestamp = timestamp;
                this.TargetDeviceIds = string.IsNullOrEmpty(deviceIds) ? null : deviceIds;
                this.Topic = topic;
                this.Payload = payload;

                if (TargetDeviceIds == null)
                {
                    targets = id => true;
                }
                else
                {
                    ids = new HashSet<string>(deviceIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim()));
                    targets = id => ids.Contains(id);
                }
            }

            public DateTimeOffset Timestamp { get; private set; }
            public string Topic { get; private set; }
            public T Payload { get; private set; }
            public string TargetDeviceIds { get; private set; }

            public override bool Equals(object obj)
            {
                var other = obj as CommandImpl<T>;
                if (other == null)
                    return false;

                return
                    this.TargetDeviceIds == other.TargetDeviceIds &&
                    this.Timestamp == other.Timestamp &&
                    this.Topic == other.Topic &&
                    this.Payload.Equals(other.Payload);
            }

            public override int GetHashCode()
            {
                return
                    (this.TargetDeviceIds == null ? 0 : this.TargetDeviceIds.GetHashCode()) ^ 
                    this.Topic.GetHashCode() ^
                    this.Timestamp.GetHashCode() ^ 
                    this.Payload.GetHashCode();
            }

            public bool TargetsDevice(string deviceId)
            {
                return targets(deviceId);
            }

            public override string ToString()
            {
                return "Command<" + typeof(T).Name + ">('" + this.Topic + "'" + 
                    (TargetDeviceIds == null ? " " :  "(" + TargetDeviceIds + ")") +
                    (typeof(T) == typeof(Unit) ? "" : ", " + this.Payload.ToString()) + 
                    ")";
            }
        }
    }
}