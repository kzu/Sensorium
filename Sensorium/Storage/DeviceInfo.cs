namespace Sensorium
{
    using System;
    using System.Linq;

    public class DeviceInfo : IDevice
    {
        public DeviceInfo(string id, string type)
        {
            this.Id = id;
            this.Type = type;
        }

        public string Id { get; private set; }
        public string Type { get; private set; }

        public IObservable<ISensed> Impulses
        {
            get { throw new NotSupportedException(); }
        }

        public void Send(IDo command)
        {
            throw new NotSupportedException();
        }
    }
}