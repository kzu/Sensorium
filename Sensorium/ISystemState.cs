namespace Sensorium
{
    using System;
    using System.Linq;

    public interface ISystemState
    {
        void Set<T>(string topic, string device, T value);
        bool Equals<T>(string topic, string device, T value);
        IQueryable<T> Of<T>(string topic, string optionalDeviceIds);
    }
}