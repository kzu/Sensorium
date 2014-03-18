namespace Sensorium
{
    using System;

    public interface ICommandStore
    {
        void Save(IDevice source, IssuedCommand issued);
    }
}