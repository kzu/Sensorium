namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Runtime.Serialization.Formatters;
    using Newtonsoft.Json;
    using Xunit;

    public class FileImpulseStoreFixture
    {
        [Fact]
        public void when_saving_bool_impulse_then_succeeds()
        {
            when_saving_impulse_then_succeeds(Impulse.Create("t", true, DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_number_impulse_then_succeeds()
        {
            when_saving_impulse_then_succeeds(Impulse.Create("t", 20f, DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_string_impulse_then_succeeds()
        {
            when_saving_impulse_then_succeeds(Impulse.Create("t", "foo", DateTimeOffset.Now));
        }

        [Fact]
        public void when_saving_void_impulse_then_succeeds()
        {
            when_saving_impulse_then_succeeds(Impulse.Create("t", DateTimeOffset.Now));
        }

        [Fact]
        public void when_reading_multiple_then_orders_by_timestamp_ascending()
        {
            var impulses = new List<IImpulse>();
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var store = new FileImpulseStore(dir);

            impulses.Add(Impulse.Create("t", new DateTimeOffset(2013, 3, 1, 00, 00, 00, TimeSpan.Zero)));
            impulses.Add(Impulse.Create("t", new DateTimeOffset(2013, 3, 1, 00, 10, 00, TimeSpan.Zero)));
            impulses.Add(Impulse.Create("t", new DateTimeOffset(2013, 3, 15, 00, 00, 00, TimeSpan.Zero)));
            impulses.Add(Impulse.Create("t", new DateTimeOffset(2013, 4, 10, 15, 00, 00, TimeSpan.Zero)));
            impulses.Add(Impulse.Create("t", new DateTimeOffset(2013, 4, 10, 22, 00, 00, TimeSpan.Zero)));

            impulses.ForEach(i => store.Save(new TestDevice("id", "type"), i));

            var saved = store.ReadAll().Select(x => x.EventArgs).ToList();

            Assert.True(Enumerable.SequenceEqual(impulses, saved));
        }

        private void when_saving_impulse_then_succeeds(IImpulse impulse)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var store = new FileImpulseStore(dir);

            store.Save(new TestDevice("id", "type"), impulse);
            var saved = store.ReadAll().First();

            Assert.Equal("id", saved.Sender.Id);
            Assert.Equal("type", saved.Sender.Type);
            Assert.Equal(impulse, saved.EventArgs);
        }
    }
}