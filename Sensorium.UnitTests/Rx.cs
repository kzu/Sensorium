namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class RxTests
    {
        [Fact]
        public void when_sliding_timeout_then_only_receives_final_window()
        {
            int TimeoutSeconds = 5;
            var clock = Clock.Default;
            
            var ticks = new SerialDisposable();
            var timeout = new SerialDisposable();
            var closer = new Subject<Unit>();
            var timeouts = 0;
            
            ticks.Disposable = clock.Tick
                .Window(TimeSpan.FromSeconds(TimeoutSeconds))
                .Subscribe(window => timeout.Disposable = window.Subscribe(_ => { }, () => timeouts++));

            Thread.Sleep(2000);

            ticks.Disposable = clock.Tick
                .Window(TimeSpan.FromSeconds(TimeoutSeconds))
                .Subscribe(window => timeout.Disposable = window.Subscribe(_ => { }, () => timeouts++));

            Thread.Sleep(6000);

            Console.WriteLine(timeouts);
        }
    }
}
