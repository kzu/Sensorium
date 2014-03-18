namespace Sensorium.UnitTests.Consumers
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Moq;
    using Xunit;
    using Xunit.Extensions;

    public class ClockImpulsesFixture
    {
        [Theory]
        [InlineData("2013/4/3 10:30:20", Topics.System.Day, 3)]
        [InlineData("2013/4/3 10:30:20", Topics.System.Month, 4)]
        [InlineData("2013/4/3 10:30:20", Topics.System.Year, 2013)]
        [InlineData("2013/4/3 10:30:20", Topics.System.Hour, 10)]
        [InlineData("2013/4/3 10:30:20", Topics.System.Minute, 30)]
        [InlineData("2013/4/3 10:30:20", Topics.System.Second, 20)]
        public void when_connected_then_pulses_system_datetime_as_individual_components(string date, string topic, int expected)
        {
            var clock = new Subject<DateTimeOffset>();
            var stream = new EventStream();
            var converter = new ClockImpulses(Mock.Of<IClock>(x => x.Tick == clock));
            converter.Connect(stream);

            var tick = DateTime.Parse(date);
            var actual = 0;

            stream.Of<IImpulse<int>>().Where(x => x.Topic == topic).Subscribe(x => actual = x.Payload);

            clock.OnNext(tick);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void when_connected_then_pulses_system_date_as_datetimeoffset()
        {
            var clock = new Subject<DateTimeOffset>();
            var stream = new EventStream();
            var converter = new ClockImpulses(Mock.Of<IClock>(x => x.Tick == clock));
            converter.Connect(stream);

            var tick = DateTimeOffset.Parse("2013/4/3 10:30:20-0300");
            var actual = DateTimeOffset.MinValue;

            stream.Of<IImpulse<DateTimeOffset>>().Where(x => x.Topic == Topics.System.Date).Subscribe(x => actual = x.Payload);

            clock.OnNext(tick);

            Assert.Equal(tick, actual);
        }

        [Fact]
        public void when_connected_then_pulses_system_time_as_timespan()
        {
            var clock = new Subject<DateTimeOffset>();
            var stream = new EventStream();
            var converter = new ClockImpulses(Mock.Of<IClock>(x => x.Tick == clock));
            converter.Connect(stream);

            var actual = TimeSpan.Zero;
            var tick = DateTime.Parse("10:30:20");

            stream.Of<IImpulse<TimeSpan>>().Where(x => x.Topic == Topics.System.Time).Subscribe(x => actual = x.Payload);

            clock.OnNext(tick);

            Assert.Equal(new TimeSpan(tick.Hour, tick.Minute, tick.Second), actual);
        }
    }
}