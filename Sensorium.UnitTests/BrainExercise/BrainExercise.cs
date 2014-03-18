namespace Sensorium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Moq;
    using Xunit.Extensions;
    using Sensorium;
    using Sensorium.Expressions;
    using Sprache;
    using System.IO;
    using Xunit;
    using System.Text;
    using Sensorium.Properties;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Runtime.CompilerServices;

    public class BrainExercise : Fixture
    {
        [Fact]
        public void SendingBytesBoolWorks()
        {
            Run();
        }

        [Fact]
        public void SendingBytesIntWorks()
        {
            Run();
        }

        [Fact]
        public void SendMessageIfColdInKidsRoomAndSomeoneIsThere()
        {
            Run();
        }

        [Fact]
        public void SendMultipleMessagesOnCondition()
        {
            Run();
        }

        [Fact]
        public void TurnOffAcIfItsColdInKidsRoom()
        {
            Run();
        }

        [Fact]
        public void WhenConditionNotMetDoesNotSend()
        {
            Run();
        }

        [Fact]
        public void WhenCurrentStateChangedThenDoesNotSendMessage()
        {
            Run();
        }

        private void Run(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = memberName + ".txt";
            Run(Parser.Process(fileName, File.ReadAllText(Path.Combine(this.GetType().Name, fileName))));
        }

        private void Run(Setup setup)
        {
            var stream = new EventStream();
            stream.Of<IEventPattern<IDevice, IImpulse>>().Subscribe(x => Tracer.Get<BrainExercise>().Info("Impulse from {0}: {1}", x.Sender.Id, x.EventArgs));
            stream.Of<ICommand<float>>().Subscribe(x => Tracer.Get<BrainExercise>().Info("Command: {0}", x));
            stream.Of<ICommand<bool>>().Subscribe(x => Tracer.Get<BrainExercise>().Info("Command: {0}", x));
            stream.Of<ICommand<string>>().Subscribe(x => Tracer.Get<BrainExercise>().Info("Command: {0}", x));
            stream.Of<ICommand<Unit>>().Subscribe(x => Tracer.Get<BrainExercise>().Info("Command: {0}", x));


            var devices = Mock.Of<IDeviceRegistry>();
            var topics = setup.Topics;
            var state = new SystemState();

            // Hook up event stream consumers that perform orthogonal operations.
            new ClockImpulses(Sensorium.Clock.Default).Connect(stream);
            new CommandToBytes().Connect(stream);
            new SensedToImpulse(Sensorium.Clock.Default, topics).Connect(stream);
            new SetSystemState(state).Connect(stream);

            var brain = new Brain(stream, devices, topics, state, Sensorium.Clock.Default);
            var evaluator = CodeEvaluator.Create();
            var anyDeviceType = Guid.NewGuid().ToString();
            var anyDevice = Mock.Of<IDevice>(d => d.Id == Guid.NewGuid().ToString() && d.Type == anyDeviceType);

            // Arrange behaviors
            foreach (var when in setup.Statements)
            {
                brain.Behave(when);
            }

            var tab = "    ";
            // Arrange verifications
            var verifications = SetupVerifications(setup, stream, evaluator, tab);

            // Act by issuing messages.
            SendGivenMessages(setup, stream, anyDeviceType, anyDevice);

            // Assert verifications succeeded.
            verifications.ForEach(v => Assert.True(v.Succeeded, "Verification failed for: " + v.Expression));
        }

        private static byte[] ParseHex(string hexString)
        {
            int length = hexString.Length / 2;
            byte[] ret = new byte[length];
            for (int i = 0, j = 0; i < length; i++)
            {
                int high = ParseNybble(hexString[j++]);
                int low = ParseNybble(hexString[j++]);
                ret[i] = (byte)((high << 4) | low);
            }

            return ret;
        }

        private static int ParseNybble(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            c = (char)(c & ~0x20);
            if (c >= 'A' && c <= 'F')
            {
                return c - ('a' - 10);
            }
            throw new ArgumentException("Invalid nybble: " + c);
        }

        private static void SendGivenMessages(Setup setup, EventStream stream, string anyDeviceType, IDevice anyDevice)
        {
            foreach (var given in setup.Given)
            {
                var type = setup.Topics.Find(given.Topic);
                Assert.True(type != TopicType.Unknown, "Undefined given topic " + given.Topic);
                ISensed impulse = null;
                // Special [bytes] format
                if (type == TopicType.Void)
                    impulse = new Sensed(given.Topic, new byte[0]);
                else if (given.Value.StartsWith("[") && given.Value.EndsWith("]"))
                    impulse = new Sensed(given.Topic,
                        ParseHex(given.Value.Substring(1, given.Value.Length - 1).Replace(" ", "")));
                else
                {
                    switch (type)
                    {
                        case TopicType.Boolean:
                            impulse = new Sensed(given.Topic, Payload.ToBytes(bool.Parse(given.Value)));
                            break;
                        case TopicType.Number:
                            impulse = new Sensed(given.Topic, Payload.ToBytes(float.Parse(given.Value)));
                            break;
                        case TopicType.String:
                            impulse = new Sensed(given.Topic, Payload.ToBytes(given.Value));
                            break;
                    }
                }

                if (string.IsNullOrEmpty(given.Devices))
                {
                    var usedGiven = setup.ParsedStatements.SelectMany(s => s.When
                        .Where(w => w.Topic == given.Topic && !string.IsNullOrEmpty(w.Devices))
                        .Select(w => w));
                    Assert.False(usedGiven.Any(), string.Format("Invalid configuration. Cannot set given impulse {0} to be emitted with no specific Device Id because impulses of the same topic are expected to be issued with device ids {1}.",
                        given.ToString(),
                        string.Join(", ", usedGiven.Select(w => w.Devices.ToString()))));

                    stream.Push(anyDevice, impulse);
                }
                else
                {
                    // Must issue one impulse for each of the given originating devices.
                    given.Devices
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList()
                        .ForEach(id => stream.Push(
                            Mock.Of<IDevice>(d => d.Id == id && d.Type == anyDeviceType),
                            impulse));
                }
            }
        }

        private static List<Verification> SetupVerifications(Setup setup, EventStream stream, Mono.CSharp.Evaluator evaluator, string tab)
        {
            var verifications = new List<Verification>();

            // First set the verifications.
            foreach (var verifyList in setup.Verify)
            {
                var verifyBuilder = new StringBuilder(@"
var func = new Func<IEventStream, IObservable<Unit>>(stream =>
");

                var filters = new List<string>();
                for (int i = 0; i < verifyList.Then.Count; i++)
                {
                    var var = ((char)(97 + i)).ToString(); // 97 == 'a'
                    var verify = verifyList.Then[i];
                    var topicType = setup.Topics.Find(verify.Topic);
                    Assert.True(topicType != TopicType.Unknown, "Undeclared topic '" + verify.Topic + "'");

                    var codeType = Brain.TopicCodeTypes.Find(topicType);

                    Assert.False((topicType == TopicType.Void && verify.Value != null),
                        string.Format("Cannot specify a value to compare for void topic '{0}' in verification '{1}'", verify.Topic, verify));

                    verifyBuilder.Append(tab).AppendLine("from {var} in stream.Commands<{type}>(\"{topic}\", \"{devices}\")".FormatWith(
                        new { var = var, type = codeType, topic = verify.Topic, devices = verify.Devices }));

                    // For void topics there's no need to filter out payload values, 
                    // just getting a message with the given topic satisfies the query.
                    if (topicType != TopicType.Void)
                    {
                        // In this case we need to take into account the value 
                        // received. 
                        filters.Add("{var} == {value}".FormatWith(
                            new { var = var, value = FormatValue(topicType, verify.Value) }));
                    }
                }

                if (filters.Count > 0)
                {
                    verifyBuilder.Append(tab).Append("where ");
                    if (verifyList.Negate)
                    {
                        verifyBuilder
                            .Append("!(")
                            .Append(string.Join(" && ", filters))
                            .AppendLine(")");
                    }
                    else
                    {
                        verifyBuilder.AppendLine(string.Join(" && ", filters));
                    }
                }

                verifyBuilder
                    .Append(tab).AppendLine("select Unit.Default")
                    .AppendLine(");")
                    .AppendLine()
                    .Append("func;");

                Tracer.Get<BrainExercise>().Verbose("Buit verification query: " + verifyBuilder.ToString());

                var func = (Func<IEventStream, IObservable<Unit>>)evaluator.Evaluate(verifyBuilder.ToString());
                var query = func(stream);
                var verification = new Verification
                {
                    Expression = (verifyList.Negate ? "!" : "") +
                        string.Join(" and ", verifyList.Then.Select(t => t.ToString())),
                    Succeeded = verifyList.Negate,
                };

                if (verifyList.Negate)
                    query.Subscribe(_ => verification.Succeeded = false);
                else
                    query.Subscribe(_ => verification.Succeeded = true);

                verifications.Add(verification);
            }

            return verifications;
        }

        private static string FormatValue(TopicType type, string value)
        {
            if (type == TopicType.String)
                value = "\"" + value + "\"";
            else if (type == TopicType.Number)
                value += "f";

            return value;
        }

        private class Verification
        {
            public string Expression { get; set; }
            public bool Succeeded { get; set; }
        }

        public static class Parser
        {
            private static readonly Parser<Tuple<string, TopicType>> TopicExpression = from type in Parse.String("bool").Return(TopicType.Boolean).Or(
                                                                                           Parse.String("boolean").Return(TopicType.Boolean)).Or(
                                                                                           Parse.String("number").Return(TopicType.Number)).Or(
                                                                                           Parse.String("string").Return(TopicType.String)).Or(
                                                                                           Parse.String("void").Return(TopicType.Void)).Token()
                                                                                       from topic in Expressions.Grammar.Topic
                                                                                       select Tuple.Create(topic, type);

            private static readonly Parser<string> StatementExpression = from _ in Parse.String("when").Token()
                                                                         from w in Parse.AnyChar.Except(Parse.String("then")).Many().Text()
                                                                         from __ in Parse.String("then").Token()
                                                                         from t in Parse.CharExcept(new[] { '\r', '\n' }).Many().Text()
                                                                         select "when " + w + " then " + t;

            private static readonly Parser<IEnumerable<Then>> ThenExpression = (from t in Grammar.Topic.Except(Parse.String("verify")).Token()
                                                                                from d in Grammar.Devices
                                                                                from v in Grammar.ThenValue.Optional()
                                                                                select new Then { Topic = t, Devices = d.GetOrDefault(), Value = v.GetOrDefault() })
                                                                    .DelimitedBy(Grammar.ThenOperatorType)
                                                                     .Named("then");


            private static readonly Parser<ThenVerify> VerifyExpression = from not in Parse.Char('!').Optional()
                                                                          from then in ThenExpression
                                                                          select new ThenVerify { Negate = !not.IsEmpty, Then = then.ToList() };

            private static readonly Parser<Setup> SetupExpression = from _ in Parse.String("setup").Token()
                                                                    from topics in TopicExpression.Many()
                                                                    from statements in StatementExpression.Many()
                                                                    from __ in Parse.String("given").Token()
                                                                    from given in ThenExpression.Many()
                                                                    from ___ in Parse.String("verify").Token()
                                                                    from when in VerifyExpression.Many()
                                                                    select new Setup
                                                                    {
                                                                        Topics = topics.ToDictionary(t => t.Item1, t => t.Item2),
                                                                        Statements = statements.ToList(),
                                                                        Given = given.SelectMany(g => g).ToList(),
                                                                        Verify = when.ToList(),
                                                                    };

            public static Setup Process(string fileName, string fileContents)
            {
                try
                {
                    var setup = SetupExpression.Parse(fileContents);
                    setup.FileName = Path.GetFileNameWithoutExtension(fileName);
                    return setup;

                }
                catch (ParseException e)
                {
                    throw new ParseException("Failed to parse file " + fileName, e);
                }
            }
        }

        public class Setup
        {
            private List<Statement> parsedStatements;

            public string FileName { get; set; }
            public IDictionary<string, TopicType> Topics { get; set; }
            public IList<string> Statements { get; set; }
            public IList<Then> Given { get; set; }
            public IList<ThenVerify> Verify { get; set; }

            public List<Statement> ParsedStatements
            {
                get
                {
                    return parsedStatements ?? (parsedStatements =
                        Statements.Select(s => Grammar.Statement.Parse(s)).ToList());
                }
            }

            public override string ToString()
            {
                return ToString(false);
            }

            public string ToString(bool fullRender = false)
            {
                if (!fullRender)
                    return FileName.ToPhrase();

                var tab = "    ";

                return
                    "File: " + FileName.ToPhrase() + Environment.NewLine +
                    new string('-', 100) + Environment.NewLine +
                    "setup" + Environment.NewLine +
                    string.Join(Environment.NewLine, Topics.Select(t => tab + t.Value.ToString().ToLower() + " " + (t.Key.Contains(' ') ? "\"" + t.Key + "\"" : t.Key))) +
                    Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, Statements.Select(s => tab + s)) +
                    Environment.NewLine + "given" + Environment.NewLine +
                    string.Join(Environment.NewLine, Given.Select(g => tab + g)) +
                    Environment.NewLine + "verify" + Environment.NewLine +
                    string.Join(Environment.NewLine, Verify.Select(v =>
                        (v.Negate ? "!" : "") +
                        string.Join(Environment.NewLine, v.Then.Select(vv => tab + vv))));
            }
        }

        public class ThenVerify
        {
            public bool Negate { get; set; }
            public List<Then> Then { get; set; }
        }
    }
}