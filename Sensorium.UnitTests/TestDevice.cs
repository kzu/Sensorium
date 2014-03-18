namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Subjects;

    public class TestDevice : IDevice
    {
        public TestDevice(string id, string type)
        {
            this.Id = id;
            this.Type = type;
            this.Impulses = new Subject<ISensed>();
            this.Commands = new List<IDo>();
        }

        public string Id { get; private set; }
        public string Type { get; private set; }
        public Subject<ISensed> Impulses { get; private set; }
        public List<IDo> Commands { get; private set; }

        public void Send(IDo command)
        {
            Commands.Add(command);
        }

        IObservable<ISensed> IDevice.Impulses { get { return Impulses; } }
    }
}