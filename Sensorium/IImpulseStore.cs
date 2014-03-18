namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;

    public interface IImpulseStore
    {
        void Save(IDevice source, IImpulse impulse);
        IEnumerable<IEventPattern<IDevice, IImpulse>> ReadAll();
    }
}