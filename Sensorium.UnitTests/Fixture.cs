namespace Sensorium.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    public abstract class Fixture
    {
        static Fixture()
        {
            Trace.AutoFlush = true;
            var manager = new TracerManager();
            manager.AddListener("Sensorium", new ConsoleTraceListener { TraceOutputOptions = TraceOptions.None });

            Tracer.Initialize(manager);
        }
    }
}