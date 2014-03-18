namespace Sensorium.UnitTests
{
    using System;
    using System.Linq;
    using Xunit;

    public class SystemStateFixture
    {
        [Fact]
        public void when_state_is_set_then_can_query()
        {
            var state = new SystemState();

            var query = state.Of<float>("t", "kids").Where(v => v == 22f);

            Assert.False(query.Any());

            state.Set("t", "kids", 22f);

            Assert.True(query.Any());
        }
    }
}