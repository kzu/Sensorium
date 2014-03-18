namespace Sensorium.UnitTests.Consumers
{
    using System;
    using System.Linq;
    using System.Reactive;
    using Xunit;

    public class CommandToBytesFixture
    {
        [Fact]
        public void when_bool_command_received_then_converts_to_byte_payload()
        {
            var stream = new EventStream();
            var consumer = new CommandToBytes();
            consumer.Connect(stream);

            var actual = default(ICommand<byte[]>);
            stream.Of<ICommand<byte[]>>().Subscribe(x => actual = x);

            stream.Push(Command.Create("t", true, DateTimeOffset.Now, "kids"));

            Assert.NotNull(actual);
            Assert.Equal("t", actual.Topic);
            Assert.Equal("kids", actual.TargetDeviceIds);
            Assert.True(Payload.ToBoolean(actual.Payload));
        }

        [Fact]
        public void when_number_command_received_then_converts_to_byte_payload()
        {
            var stream = new EventStream();
            var consumer = new CommandToBytes();
            consumer.Connect(stream);

            var actual = default(ICommand<byte[]>);
            stream.Of<ICommand<byte[]>>().Subscribe(x => actual = x);

            stream.Push(Command.Create("t", 20f, DateTimeOffset.Now, "kids"));

            Assert.NotNull(actual);
            Assert.Equal("t", actual.Topic);
            Assert.Equal("kids", actual.TargetDeviceIds);
            Assert.Equal(20f, Payload.ToNumber(actual.Payload));
        }

        [Fact]
        public void when_string_command_received_then_converts_to_byte_payload()
        {
            var stream = new EventStream();
            var consumer = new CommandToBytes();
            consumer.Connect(stream);

            var actual = default(ICommand<byte[]>);
            stream.Of<ICommand<byte[]>>().Subscribe(x => actual = x);

            stream.Push(Command.Create("t", "foo", DateTimeOffset.Now, "kids"));

            Assert.NotNull(actual);
            Assert.Equal("t", actual.Topic);
            Assert.Equal("kids", actual.TargetDeviceIds);
            Assert.Equal("foo", Payload.ToString(actual.Payload));
        }
    }
}