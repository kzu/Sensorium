namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Runtime.Serialization.Formatters;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class FileCommandStoreFixture
    {
        [Fact]
        public void when_saving_bool_command_then_succeeds()
        {
            when_saving_command_then_succeeds(Command.Create("t", true, DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_number_command_then_succeeds()
        {
            when_saving_command_then_succeeds(Command.Create("t", 20f, DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_string_command_then_succeeds()
        {
            when_saving_command_then_succeeds(Command.Create("t", "foo", DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_void_command_then_succeeds()
        {
            when_saving_command_then_succeeds(Command.Create("t", DateTimeOffset.Now));
        }

        [Fact]
        public void when_reading_multiple_then_orders_by_timestamp_ascending()
        {
            var commands = new List<ICommand>();
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var store = new FileCommandStore(dir);

            commands.Add(Command.Create("t", new DateTimeOffset(2013, 3, 1, 00, 00, 00, TimeSpan.Zero)));
            commands.Add(Command.Create("t", new DateTimeOffset(2013, 3, 1, 00, 10, 00, TimeSpan.Zero)));
            commands.Add(Command.Create("t", new DateTimeOffset(2013, 3, 15, 00, 00, 00, TimeSpan.Zero)));
            commands.Add(Command.Create("t", new DateTimeOffset(2013, 4, 10, 15, 00, 00, TimeSpan.Zero)));
            commands.Add(Command.Create("t", new DateTimeOffset(2013, 4, 10, 22, 00, 00, TimeSpan.Zero)));

            commands.ForEach(i => store.Save(new TestDevice("id", "type"), new IssuedCommand(i, "when foo then bar")));

            var saved = store.ReadAll().Select(x => x.EventArgs).ToList();
            var comparer = new Mock<IEqualityComparer<ICommand>>();
            comparer.Setup(x => x.Equals(It.IsAny<ICommand>(), It.IsAny<ICommand>()))
                .Returns<ICommand, ICommand>((x, y) => x.Topic == y.Topic && x.Timestamp == y.Timestamp && x.GetType() == y.GetType());

            Assert.True(Enumerable.SequenceEqual(commands, saved, comparer.Object));
        }

        private void when_saving_command_then_succeeds(ICommand command)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var store = new FileCommandStore(dir);

            store.Save(new TestDevice("id", "type"), new IssuedCommand(command, "when foo then bar"));
            var saved = store.ReadAll().First();

            Assert.Equal("id", saved.Sender.Id);
            Assert.Equal("type", saved.Sender.Type);
            Assert.Equal(command.Topic, saved.EventArgs.Topic);
            Assert.Equal(command.Timestamp, saved.EventArgs.Timestamp);
            // NOTE: the target device id is changed to the actual device id.
            Assert.Equal(saved.EventArgs.TargetDeviceIds, "id");
        }
    }
}