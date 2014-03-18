namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using Xunit;
    using Xunit.Extensions;
    using SharpTestsEx;
    using Sensorium.Expressions;
    using System.IO;
    using System.Linq.DynamicQuery;

    public class ExpressionsFixture
    {
        [Fact]
        public void when_parsing_constant_then_succeeds()
        {
            var input = " when   ";
            var parser = Parse.String("when").Text().Token();

            var output = parser.Parse(input);

            Assert.Equal("when", output);
        }

        [Theory]
        [InlineData("control", "control")]
        [InlineData("control/foo/temp", "control/foo/temp")]
        [InlineData("control/foo_temp", "control/foo_temp")]
        [InlineData("control/foo whitespace not included", "control/foo")]
        public void when_parsing_topic_then_succeeds(string input, string output)
        {
            Assert.Equal(output, Grammar.Topic.Parse(input));
        }

        [Theory]
        [InlineData(" == ", Comparison.Equal)]
        [InlineData("==", Comparison.Equal)]
        [InlineData(" != ", Comparison.NotEqual)]
        [InlineData("<", Comparison.LessThan)]
        [InlineData(">", Comparison.GreaterThan)]
        public void when_parsing_comparison_then_succeeds(string input, Comparison comparison)
        {
            var output = Grammar.ComparisonType.Parse(input);

            Assert.Equal(comparison, output);
        }

        [Theory]
        [InlineData("control/kids/temp < 24", "control/kids/temp", null, Comparison.LessThan, "24")]
        [InlineData(@"control/kids/temp   <24  ", "control/kids/temp", null, Comparison.LessThan, "24")]
        [InlineData("control/kids/temp == 24", "control/kids/temp", null, Comparison.Equal, "24")]
        [InlineData("temp(kidsRoom) == 24", "temp", "kidsRoom", Comparison.Equal, "24")]
        [InlineData("temp(kidsRoom,mainRoom) == 24", "temp", "kidsRoom,mainRoom", Comparison.Equal, "24")]
        public void when_parsing_when_then_succeeds(string input, string topic, string devices, Comparison comparison, string value)
        {
            var output = Grammar.When.Parse(input);

            Assert.Equal(topic, output.First().Topic);
            Assert.Equal(devices, output.First().Devices);
            Assert.Equal(comparison, output.First().Comparison);
            Assert.Equal(value, output.First().Value);
        }

        [Theory]
        [InlineData("control/kids/temp =21", "control/kids/temp", "21")]
        [InlineData("control/kids/off", "control/kids/off", null)]
        [InlineData("control/kids/temp=  24", "control/kids/temp", "24")]
        public void when_parsing_then_then_succeeds(string input, string topic, string value)
        {
            var output = Grammar.Then.Parse(input);

            Assert.Equal(topic, output.First().Topic);
            Assert.Equal(value, output.First().Value);
        }

        [Fact]
        public void when_parsing_multiple_thens_then_succeeds()
        {
            var output = Grammar.Then.Parse("mg = hot && of(kitchen)").ToList();

            Assert.Equal(2, output.Count);
            Assert.Equal("mg", output[0].Topic);
            Assert.Equal("hot", output[0].Value);
            Assert.Equal("of", output[1].Topic);
            Assert.Equal("kitchen", output[1].Devices);
        }

        [Theory]
        [InlineData("when sensed/kids/temp < 23   then    control/kids/off", "sensed/kids/temp", Comparison.LessThan, "23", "control/kids/off", null)]
        [InlineData("when sensed/kids/temp == 22 then control/kids/on", "sensed/kids/temp", Comparison.Equal, "22", "control/kids/on", null)]
        [InlineData("when sensed/kids/temp == 22 then control/kids/temp = 21", "sensed/kids/temp", Comparison.Equal, "22", "control/kids/temp", "21")]
        public void when_parsing_statement_then_succeeds(string input, string when, Comparison comparison, string whenValue, string then, string thenValue)
        {
            var statement = Grammar.Statement.Parse(input);

            Assert.Equal(when, statement.When.First().Topic);
            Assert.Equal(comparison, statement.When.First().Comparison);
            Assert.Equal(whenValue, statement.When.First().Value);
            Assert.Equal(then, statement.Then.First().Topic);
            Assert.Equal(thenValue, statement.Then.First().Value);
        }

        [Fact]
        public void when_parsing_statements_then_succeeds()
        {
            var input = @"
when sensed/kidsAC/temp < 22 then control/kidsAC/off
when sensed/kidsAC/temp > 25 then control/kidsAC/on
when sensed/kidsAC/temp > 25 then control/kidsAC/temp = 23";

            var output = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Grammar.Statement.Parse(line))
                .ToList();

            Assert.Equal(3, output.Count);

            output[0].Satisfy(s =>
                s.When.First().Topic == "sensed/kidsAC/temp" &&
                s.When.First().Comparison == Comparison.LessThan &&
                s.When.First().Value == "22" &&
                s.Then.First().Topic == "control/kidsAC/off" &&
                s.Then.First().Value == null);

            output[1].Satisfy(s =>
                s.When.First().Topic == "sensed/kidsAC/temp" &&
                s.When.First().Comparison == Comparison.GreaterThan &&
                s.When.First().Value == "25" &&
                s.Then.First().Topic == "control/kidsAC/on" &&
                s.Then.First().Value == null);

            output[2].Satisfy(s =>
                s.When.First().Topic == "sensed/kidsAC/temp" &&
                s.When.First().Comparison == Comparison.GreaterThan &&
                s.When.First().Value == "25" &&
                s.Then.First().Topic == "control/kidsAC/temp" &&
                s.Then.First().Value == "23");
        }

        [Theory]
        [ExpressionData("ExpressionsFixture.txt")]
        public void when_parsing_expressions_from_file_then_verifies_successfully(string behaviorExpression, string verificationExpression)
        {
            try
            {
                var statement = Grammar.Statement.Parse(behaviorExpression);
                var expression = DynamicExpression.ParseLambda<Statement, bool>(verificationExpression, statement);

                statement.Satisfy(expression);
            }
            catch (Sprache.ParseException e)
            {
                throw new Sprache.ParseException("Failed to parse behavior expression '" + behaviorExpression + "'", e);
            }
            catch (System.Linq.DynamicQuery.ParseException e)
            {
                throw new Sprache.ParseException("Failed to parse verification expression '" + verificationExpression + "'", e);
            }
        }

        public class ExpressionData : DataAttribute
        {
            private string dataFile;

            public ExpressionData(string dataFile)
            {
                this.dataFile = dataFile;
            }

            public override IEnumerable<object[]> GetData(System.Reflection.MethodInfo methodUnderTest, Type[] parameterTypes)
            {
                var parser = (from statement in Parse.CharExcept('|').AtLeastOnce().Text()
                              from pipe in Parse.Char('|')
                              from verification in Parse.CharExcept(';').AtLeastOnce().Text()
                              select new { Statement = statement.Trim(), Verification = verification.Trim() })
                             .DelimitedBy(Parse.Char(';'));

                var statements = parser.Parse(String.Join(Environment.NewLine, 
                    File.ReadAllLines(dataFile).Where(line => !line.Trim().StartsWith("//")))).ToList();

                return statements.Select(x => new object[] 
                { 
                    x.Statement, 
                    x.Verification,
                });
            }
        }
    }
}