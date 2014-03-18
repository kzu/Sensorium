namespace Sensorium
{
    using System;
    using System.Collections.Generic;

    public interface IDeviceRegistry
    {
        IEnumerable<string> GetCommands(string deviceType);
        //IEnumerable<string> GetImpulses(string deviceType);
    }
}