namespace System.Diagnostics
{
    partial class Tracer
    {
        public static ITracerManager Manager { get { return manager; } }

        partial class DefaultManager
        {
            public void SetTracingLevel(string sourceName, SourceLevels level) { }
        }
    }

    partial interface ITracerManager
    {
        void SetTracingLevel(string sourceName, SourceLevels level);
    }
}