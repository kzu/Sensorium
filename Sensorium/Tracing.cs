namespace Sensorium
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Sensorium.Properties;

    public static class Tracing
    {
        public static class Brain
        {
            private static readonly ITracer tracer = Tracer.Get<Sensorium.Brain>();

            public static void ConfiguringBehavior(string behavior)
            {
                tracer.Info(Strings.Brain.ConfiguringBehavior(behavior));
            }

            public static void BuiltBehaviorQuery(string when, string query)
            {
                tracer.Verbose(Strings.Brain.BuiltBehaviorQuery(when, query));
            }

            public static void BuiltStateQuery(string when, string query)
            {
                tracer.Verbose(Strings.Brain.BuiltStateQuery(when, query));
            }

            public static void BuiltBehaviorAction(string then, string action)
            {
                tracer.Verbose(Strings.Brain.BuiltBehaviorAction(then, action));
            }

            public static void MatchedBehaviorCondition(string behavior)
            {
                tracer.Info(Strings.Brain.MatchedBehaviorCondition(behavior));
            }

            public static void ExecutingBehaviorAction(string behavior)
            {
                tracer.Info(Strings.Brain.ExecutingBehaviorAction(behavior));
            }
        }

        public static void Trace(string sourceName, TraceEventType type, string format, params object[] args)
        {
            Tracer.Get(sourceName).Trace(type, format, args);
        }
    }
}