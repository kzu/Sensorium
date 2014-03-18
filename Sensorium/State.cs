namespace Sensorium
{
    using System;
    using System.Linq;

    public class State<T>
    {
        public string Device { get; set; }
        public T Value { get; set; }
    }
}