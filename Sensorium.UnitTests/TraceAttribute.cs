namespace Sensorium.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class TraceAttribute : BeforeAfterTestAttribute
    {
        private string source;
        private SourceLevels level;

        public TraceAttribute(string sourceName, SourceLevels level)
        {
            this.source = sourceName;
            this.level = level;
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            base.Before(methodUnderTest);
            Tracer.Manager.SetTracingLevel(this.source, this.level);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            base.After(methodUnderTest);

            Tracer.Manager.SetTracingLevel(this.source, SourceLevels.Off);
        }
    }
}