namespace Sensorium.UnitTests.Consumers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Text;
    using Moq;
    using Xunit;

    public class SensedToImpulseFixture
    {
        [Fact]
        public void when_pushing_sensed_then_converts_to_number_impulse()
        {
            var stream = new EventStream();
            var topics = new Dictionary<string, TopicType>
            {
                { "t", TopicType.Number },
            };

            var temp = (float?)null;

            stream.Of<IImpulse<float>>().Subscribe(i => temp = i.Payload);

            new SensedToImpulse(Clock.Default, topics).Connect(stream);

            stream.Push(Mock.Of<IDevice>(x => x.Id == "foo"), new Sensed("t", Payload.ToBytes(22f)));

            Assert.Equal(22f, temp.GetValueOrDefault());
        }

        [Fact]
        public void when_pushing_sensed_then_converts_to_boolean_impulse()
        {
            var stream = new EventStream();
            var topics = new Dictionary<string, TopicType>
            {
                { "t", TopicType.Boolean },
            };

            var temp = (bool?)null;

            stream.Of<IImpulse<bool>>().Subscribe(i => temp = i.Payload);

            new SensedToImpulse(Clock.Default, topics).Connect(stream);

            stream.Push(Mock.Of<IDevice>(x => x.Id == "foo"), new Sensed("t", Payload.ToBytes(true)));

            Assert.True(temp.GetValueOrDefault());
        }

        [Fact]
        public void when_pushing_sensed_then_converts_to_string_impulse()
        {
            var stream = new EventStream();
            var topics = new Dictionary<string, TopicType>
            {
                { "t", TopicType.String },
            };

            var temp = default(string);

            stream.Of<IImpulse<string>>().Subscribe(i => temp = i.Payload);

            new SensedToImpulse(Clock.Default, topics).Connect(stream);

            stream.Push(Mock.Of<IDevice>(x => x.Id == "foo"), new Sensed("t", Encoding.UTF8.GetBytes("foo")));

            Assert.Equal("foo", temp);
        }

        [Fact]
        public void when_pushing_sensed_then_converts_to_void_impulse()
        {
            var stream = new EventStream();
            var topics = new Dictionary<string, TopicType>
            {
                { "t", TopicType.Void },
            };

            var temp = (Unit?)null;

            stream.Of<IImpulse<Unit>>().Subscribe(i => temp = i.Payload);

            new SensedToImpulse(Clock.Default, topics).Connect(stream);

            stream.Push(Mock.Of<IDevice>(x => x.Id == "foo"), new Sensed("t", new byte[0]));

            Assert.True(temp.HasValue);
            Assert.Equal(Unit.Default, temp.Value);
        }
    }
}