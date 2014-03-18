namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using Mono.CSharp;

    public static class CodeEvaluator
    {
        public static Evaluator Create()
        {
            var evaluator = new Evaluator(new CompilerContext(new CompilerSettings(), new ConsoleReportPrinter()));

            evaluator.ReferenceAssembly(typeof(CodeEvaluator).Assembly);
            // Rx Core
            evaluator.ReferenceAssembly(typeof(ObservableExtensions).Assembly);
            // Rx Interfaces
            evaluator.ReferenceAssembly(typeof(IEventSource<>).Assembly);
            // Rx Linq
            evaluator.ReferenceAssembly(typeof(Observable).Assembly);

            evaluator.Run(@"
                using System;
                using System.Linq;
                using System.Linq.Expressions;
                using System.Reactive;
                using System.Reactive.Linq;
                using Sensorium;
            ");

            return evaluator;
        }
    }
}