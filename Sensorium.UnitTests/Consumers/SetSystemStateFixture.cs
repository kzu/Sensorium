namespace Sensorium.UnitTests.Consumers
{
    using System;
    using System.Linq;
    using System.Reactive;
    using Moq;
    using Xunit;

    public class SetSystemStateFixture
    {
        [Fact]
        public void when_boolean_impulse_received_then_sets_system_state()
        {
            var stream = new EventStream();
            var state = new SystemState();
            var consumer = new SetSystemState(state);
            consumer.Connect(stream);

            Assert.False(state.Of<bool>("t", "kids").Any());

            stream.Push(Mock.Of<IDevice>(x => x.Id == "kids"), Impulse.Create("t", true, DateTimeOffset.Now));

            Assert.True(state.Of<bool>("t", "kids").FirstOrDefault());            
        }

        [Fact]
        public void when_number_impulse_received_then_sets_system_state()
        {
            var stream = new EventStream();
            var state = new SystemState();
            var consumer = new SetSystemState(state);
            consumer.Connect(stream);

            Assert.False(state.Of<float>("t", "kids").Any());

            stream.Push(Mock.Of<IDevice>(x => x.Id == "kids"), Impulse.Create("t", 20f, DateTimeOffset.Now));

            Assert.Equal(20f, state.Of<float>("t", "kids").FirstOrDefault());
        }

        [Fact]
        public void when_string_impulse_received_then_sets_system_state()
        {
            var stream = new EventStream();
            var state = new SystemState();
            var consumer = new SetSystemState(state);
            consumer.Connect(stream);

            Assert.False(state.Of<string>("t", "kids").Any());

            stream.Push(Mock.Of<IDevice>(x => x.Id == "kids"), Impulse.Create("t", "foo", DateTimeOffset.Now));

            Assert.Equal("foo", state.Of<string>("t", "kids").FirstOrDefault());
        }
    }
}