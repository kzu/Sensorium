namespace Sensorium.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;
    using Xunit.Extensions;

    public class MessageChannelFixture
    {
        [Fact]
        public void when_serializing_connect_then_can_roundtrip()
        {
            var message = new Connect("foo", "bar");
            var bytes = MessageChannel.Convert(message);

            var deserialized = MessageChannel.Convert(bytes) as Connect;

            Assert.NotNull(deserialized);
            Assert.Equal("foo", deserialized.DeviceId);
            Assert.Equal("bar", deserialized.DeviceType);
        }

        [Fact]
        public void when_serializing_disconnect_then_can_roundtrip()
        {
            var message = new Disconnect();
            var bytes = MessageChannel.Convert(message);

            var deserialized = MessageChannel.Convert(bytes) as Disconnect;

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void when_serializing_ping_then_can_roundtrip()
        {
            var message = new Ping();
            var bytes = MessageChannel.Convert(message);

            var deserialized = MessageChannel.Convert(bytes) as Ping;

            Assert.NotNull(deserialized);
        }

        [Fact]
        public void when_serializing_topic_then_can_roundtrip()
        {
            var message = new Topic("foo", Encoding.UTF8.GetBytes("bar"));
            var bytes = MessageChannel.Convert(message);

            var deserialized = MessageChannel.Convert(bytes) as Topic;

            Assert.NotNull(deserialized);
            Assert.Equal("foo", deserialized.Name);
            Assert.True(Enumerable.SequenceEqual(message.Payload, deserialized.Payload));
        }
    }
}