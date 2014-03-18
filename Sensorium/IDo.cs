namespace Sensorium
{
    using System;

    public interface IDo
    {
        string Topic { get; }
        byte[] Payload { get; }
    }
}