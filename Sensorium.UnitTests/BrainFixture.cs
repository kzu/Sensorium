namespace Sensorium.UnitTests
{
    using Xunit;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Linq;
    using Xunit.Extensions;
    using System.Reactive.Subjects;
    using System;
    using Moq;
    using Sensorium.Expressions;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Sprache;
    using System.IO;
    using System.Diagnostics;

    public class BrainFixture : Fixture
    {
        private Brain brain;
        private EventStream stream = new EventStream();
        private Dictionary<string, TopicType> topics = new Dictionary<string, TopicType>();
        private ISystemState state = new SystemState();
        private Mock<IDeviceRegistry> devices = new Mock<IDeviceRegistry>();
        private Subject<DateTimeOffset> clock = new Subject<DateTimeOffset>();

        public BrainFixture()
        {
            stream.Of<IEventPattern<IDevice, ISensed>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Sensed from {0}: {1}", x.Sender.Id, x.EventArgs));
            stream.Of<IEventPattern<IDevice, IImpulse>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Impulse from {0}: {1}", x.Sender.Id, x.EventArgs));
            stream.Of<ICommand<float>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Command: {0}", x));
            stream.Of<ICommand<bool>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Command: {0}", x));
            stream.Of<ICommand<string>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Command: {0}", x));
            stream.Of<ICommand<Unit>>().Subscribe(x => Tracer.Get<BrainFixture>().Info("Command: {0}", x));

            // Hook up event stream consumers that perform orthogonal operations.
            new ClockImpulses(Mock.Of<IClock>(x => x.Tick == clock)).Connect(stream);
            new CommandToBytes().Connect(stream);
            new SensedToImpulse(Sensorium.Clock.Default, topics).Connect(stream);
            new SetSystemState(state).Connect(stream);

            this.brain = new Brain(stream, devices.Object, topics, state, Mock.Of<IClock>(x => x.Tick == clock));
        }

        [Fact]
        public void when_registering_behavior_then_succeeds()
        {
            topics.Add("t", TopicType.Number);
            topics.Add("in", TopicType.Boolean);
            topics.Add("on", TopicType.Void);

            brain.Behave("when t(kids, kitchen, living) > 24 && in(kids) == true then on(ac)");
        }

        [Fact]
        public void when_behavior_matches_then_issues_command_to_device()
        {
            topics["in"] = TopicType.Boolean;
            topics["on"] = TopicType.Boolean;

            // Define supported commands by the device.
            devices.Setup(x => x.GetCommands("light")).Returns(new[] { "on" });

            var kidsRoom = new TestDevice("kidsRoom", "move");
            var kidsLight = new TestDevice("kidsLight", "light");

            brain.Connect(kidsRoom);
            brain.Connect(kidsLight);

            brain.Behave("when in(kidsRoom) == false then on(kidsLight) = false");

            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(false)));

            Assert.Equal(1, kidsLight.Commands.Count);
        }

        [Fact]
        public void when_state_matches_command_topic_and_value_then_does_not_send_command()
        {
            topics["in"] = TopicType.Boolean;
            topics["on"] = TopicType.Boolean;

            devices.Setup(x => x.GetCommands("light")).Returns(new[] { "on" });
            var kidsRoom = new TestDevice("kidsRoom", "move");
            var kidsLight = new TestDevice("kidsLight", "light");

            brain.Connect(kidsLight);

            brain.Behave("when in(kidsRoom) == false then on(kidsLight) = false");

            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(true)));
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(true)));
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(false)));

            Assert.False(state.Of<bool>("on", kidsLight.Id).Single());

            // NOTE: lights are off already when kids leave the room
            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(false)));

            // So the command is not sent.
            Assert.Equal(0, kidsLight.Commands.Count);
        }

        [Fact]
        public void when_behavior_conditions_cease_to_exist_then_sends_undo_command()
        {
            topics["in"] = TopicType.Boolean;
            topics["on"] = TopicType.Boolean;

            devices.Setup(x => x.GetCommands("light")).Returns(new[] { "on" });

            var kidsRoom = new TestDevice("kidsRoom", "move");
            var kidsLight = new TestDevice("kidsLight", "light");

            brain.Connect(kidsRoom);
            brain.Connect(kidsLight);

            brain.Behave("when in(kidsRoom) == false then on(kidsLight) = false");

            // Establish current state that needs to be reversed.
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(true)));
            
            // Kids are on when kids leave the room.
            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(false)));
            
            // At this point we have one command, that turns off the lights.
            Assert.Equal(1, kidsLight.Commands.Count);

            // We simulate the light sending that it's now off after executing 
            // the command.
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(false)));

            // When kids come back, the previous behavior no longer applies
            // so its actions are undone.
            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(true)));

            // At this point we have another command, that turns on the lights as they were 
            // before.
            Assert.Equal(2, kidsLight.Commands.Count);
            Assert.False(Payload.ToBoolean(kidsLight.Commands.First().Payload));
            Assert.True(Payload.ToBoolean(kidsLight.Commands.Last().Payload));
        }

        [Fact]
        public void when_behavior_conditions_cease_to_exist_after_subsequent_impulses_in_state_then_sends_undo_command()
        {
            topics["t"] = TopicType.Number;
            topics["on"] = TopicType.Boolean;

            devices.Setup(x => x.GetCommands("ac")).Returns(new[] { "on" });

            var ac = new TestDevice("ac", "ac");

            brain.Connect(ac);
            brain.Behave("when t(ac) > 23 then on(ac) = true");

            // Current state.
            ac.Impulses.OnNext(new Sensed("on", Payload.ToBytes(false)));
            
            // Stimuli
            ac.Impulses.OnNext(new Sensed("t", Payload.ToBytes(24f)));

            // At this point we have one command, that turns on the A/C.
            Assert.Equal(1, ac.Commands.Count);

            // State changes as a consecuence of the command.
            ac.Impulses.OnNext(new Sensed("on", Payload.ToBytes(true)));

            // Further temperature changes that do not cause new commands 
            // since state is the same
            ac.Impulses.OnNext(new Sensed("t", Payload.ToBytes(25f)));

            // Now we do sense lower than 23 temperature.
            ac.Impulses.OnNext(new Sensed("t", Payload.ToBytes(22f)));

            Assert.Equal(2, ac.Commands.Count);
            Assert.True(Payload.ToBoolean(ac.Commands.First().Payload));
            Assert.False(Payload.ToBoolean(ac.Commands.Last().Payload));
        }

        [Fact]
        public void when_sending_undo_command_then_sends_issued_command()
        {
            topics["in"] = TopicType.Boolean;
            topics["on"] = TopicType.Boolean;

            var issued = new List<IssuedCommand>();
            stream.Of<IssuedCommand>().Subscribe(x => issued.Add(x));

            devices.Setup(x => x.GetCommands("light")).Returns(new[] { "on" });

            var kidsRoom = new TestDevice("kidsRoom", "move");
            var kidsLight = new TestDevice("kidsLight", "light");

            brain.Connect(kidsRoom);
            brain.Connect(kidsLight);

            brain.Behave("when in(kidsRoom) == false then on(kidsLight) = false");
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(true)));
            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(false)));
            kidsLight.Impulses.OnNext(new Sensed("on", Payload.ToBytes(false)));
            kidsRoom.Impulses.OnNext(new Sensed("in", Payload.ToBytes(true)));

            Assert.Equal(2, issued.Count);
        }

    }
}
