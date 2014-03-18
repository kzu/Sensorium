namespace Sensorium
{
    using System;

    public interface ICommand
    {
        string Topic { get; }
        DateTimeOffset Timestamp { get; }

        string TargetDeviceIds { get; }
        bool TargetsDevice(string deviceId);
    }

    public interface ICommand<out T> : ICommand
    {
        T Payload { get; }
    }
}