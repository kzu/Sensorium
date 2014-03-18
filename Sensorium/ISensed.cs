namespace Sensorium
{
    using System;

    public interface ISensed
    {
        string Topic { get; }
        byte[] Payload { get; }
    }
}