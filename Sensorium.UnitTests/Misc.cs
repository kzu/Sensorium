namespace Sensorium.UnitTests
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Text;
    using Moq;
    using Xunit;

    public class Misc
    {
        private DateTimeOffset now = DateTimeOffset.Now;

        [Fact]
        public void when_creating_two_impulses_then_they_equal_by_structure()
        {
            var i1 = Impulse.Create<float>("foo", 23f, now);
            var i2 = Impulse.Create<float>("foo", 23f, now);

            Assert.Equal(i1, i2);
            Assert.Equal(i1.GetHashCode(), i2.GetHashCode());
            Assert.NotEmpty(i1.ToString());
        }

        [Fact]
        public void when_creating_two_commands_then_they_equal_by_structure()
        {
            var c1 = Command.Create<float>("foo", 23f, now);
            var c2 = Command.Create<float>("foo", 23f, now);

            Assert.Equal(c1, c2);
            Assert.Equal(c1.GetHashCode(), c2.GetHashCode());
            Assert.NotEmpty(c1.ToString());
        }

        private Expression<Func<T, bool>> Expr<T>(Expression<Func<T, bool>> e)
        {
            return e;
        }

        public class StreamEx
        {
            public StreamEx()
            {
            }

            public StreamEx Where(Expression<Func<StreamEx, bool>> filter)
            {
                return this;
            }

            //public void 

            public T of<T>(string topic, params string[] deviceIds)
            {
                return default(T);
            }
        }
    }
}