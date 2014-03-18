namespace Sensorium
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using Sensorium.Expressions;
    using Sprache;
    using System.Text;
    using System.Linq.Expressions;
    using System.Reflection;
    using Sensorium.Properties;
    using System.Reactive.Disposables;

    /// <summary>
    /// Implements the core matching behavior.
    /// </summary>
    public class Brain
    {
        private static readonly Regex whenThen = new Regex(@"when\s(?<when>.+)\sthen\s(?<then>.+)", RegexOptions.Compiled);
        private static readonly Regex equals = new Regex(@"\s?=\s?", RegexOptions.Compiled);

        internal static readonly IDictionary<TopicType, string> TopicCodeTypes = new Dictionary<TopicType, string>
        {
            { TopicType.Boolean, "bool" },
            { TopicType.Number, "float" },
            { TopicType.String, "string" },
            { TopicType.Void, "Unit" },
        };

        private Dictionary<IDevice, DeviceEntry> connectedDevices = new Dictionary<IDevice, DeviceEntry>();
        private HashSet<BehaviorEntry> configuredBehaviors = new HashSet<BehaviorEntry>();

        private IEventStream stream;
        private IDeviceRegistry deviceRegistry;
        private IDictionary<string, TopicType> topicRegistry;
        private ISystemState state;
        private IClock clock;
        private Mono.CSharp.Evaluator evaluator = CodeEvaluator.Create();

        public Brain(IEventStream stream, IDeviceRegistry deviceRegistry, IDictionary<string, TopicType> topicRegistry, ISystemState state, IClock clock)
        {
            this.stream = stream;
            this.deviceRegistry = deviceRegistry;
            this.topicRegistry = topicRegistry;
            this.state = state;
            this.clock = clock;
        }

        public IEventStream Stream { get { return stream; } }

        public void Connect(IDevice device)
        {
            var topics = new HashSet<string>(deviceRegistry.GetCommands(device.Type));
            var deviceId = device.Id;

            var entry = new DeviceEntry
            {
                Device = device,
                Impulses = device.Impulses.Subscribe(imp => stream.Push(device, imp)),
                // We subscribe to all four types instead of pre-converting them to 
                // ICommand<byte[]> which would be another alternative, although 
                // maybe more costly because it would increase the flowing messages 
                // and potentially slow down Rx?
                Commands = new CompositeDisposable(
                    Subscribe<bool>(device, topics),
                    Subscribe<float>(device, topics),
                    Subscribe<string>(device, topics),
                    Subscribe<Unit>(device, topics)),
            };

            connectedDevices.Add(device, entry);
        }

        public void Disconnect(IDevice device)
        {
            var entry = connectedDevices.Find(device);
            if (entry != null)
            {
                entry.Commands.Dispose();
                entry.Impulses.Dispose();
                connectedDevices.Remove(device);
            }
        }

        public void Behave(string behavior)
        {
            Tracing.Brain.ConfiguringBehavior(behavior);

            var entry = new BehaviorEntry
            {
                Behavior = behavior,
                Statement = Grammar.Statement.Parse(behavior.Trim()),
            };

            var matched = whenThen.Match(behavior);
            var tab = "    ";

            var query = BuildStreamQuery(tab, matched.Groups["when"].Value, entry.Statement.When);
            var stateCheck = BuildStateQuery(tab, matched.Groups["when"].Value, entry.Statement.When);
            var action = BuildStreamAction(tab, matched.Groups["then"].Value, entry.Statement.Then);

            // NOTE: we filter out impulses that at the time of match do not 
            // have a corresponding state set (meaning another state might 
            // have been pushed later which invalidates the previous one.
            entry.Enter = query.Do(_ => Tracing.Brain.MatchedBehaviorCondition(behavior)).Subscribe(_ =>
            {
                // If the behavior is already active, don't re-enter it.
                if (!entry.IsActive)
                {
                    // Clear exit undo commands at this point as they 
                    // are generated based on the commands that the 
                    // entry phase generates.
                    entry.UndoCommands.Clear();

                    if (stateCheck.Any())
                    {
                        Tracing.Brain.ExecutingBehaviorAction(behavior);
                        var ctx = new CommandContext("enter: " + behavior, entry.UndoCommands);
                        action(ctx, stream, state, clock);
                    }

                    entry.IsActive = true;
                }
            });

            var negated = BuildStreamQuery(tab, matched.Groups["when"].Value, entry.Statement.When, true);
            entry.Exit = negated.Subscribe(_ =>
            {
                var ctx = new CommandContext("exit " + behavior, entry.UndoCommands);
                foreach (var cmd in entry.UndoCommands.ToArray())
                {
                    // NOTE: these issued commands will again cause additional undo 
                    // commands to be placed in the list, which is why we 
                    // clear them when we are done.
                    // TODO: see how to refactor this so that the TEvent 
                    // on stream.Push is automatically handled and we don't have to 
                    // down-cast like this.
                    var boolCmd = cmd as ICommand<bool>;
                    if (boolCmd != null)
                        stream.Push(ctx, boolCmd);

                    var numberCmd = cmd as ICommand<float>;
                    if (numberCmd != null)
                        stream.Push(ctx, numberCmd);

                    var stringCmd = cmd as ICommand<string>;
                    if (stringCmd != null)
                        stream.Push(ctx, stringCmd);

                    var voidCmd = cmd as ICommand<Unit>;
                    if (voidCmd != null)
                        stream.Push(ctx, voidCmd);
                }

                // Clear after we issued them all.
                entry.UndoCommands.Clear();
                // Mark the entry as inactive again.
                entry.IsActive = false;
            });
        }

        private IDisposable Subscribe<T>(IDevice device, HashSet<string> topics)
        {
            return stream
                .Of<CommandContext, ICommand<T>>()
                .Where(cmd => topics.Contains(cmd.EventArgs.Topic) && cmd.EventArgs.TargetsDevice(device.Id))
                .Subscribe(cmd =>
                {
                    var currentState = state.Of<T>(cmd.EventArgs.Topic, device.Id);
                    var currentValue = currentState.FirstOrDefault();
                    // NOTE: this is where we could send the command anyway even if the state value matches.
                    if (currentState.Any())
                    {
                        if (!object.Equals(currentValue, cmd.EventArgs.Payload))
                        {
                            device.Send(cmd.EventArgs.ToDo());
                            // Generate the undo command from the value 
                            cmd.Sender.UndoCommands.Add(Command.Create(cmd.EventArgs.Topic, currentValue, clock.Now, device.Id));
                            stream.Push(device, new IssuedCommand(cmd.EventArgs, cmd.Sender.Behavior));
                        }
                    }
                    else
                    {
                        // Send if we don't have a known previous state.
                        device.Send(cmd.EventArgs.ToDo());
                        stream.Push(device, new IssuedCommand(cmd.EventArgs, cmd.Sender.Behavior));
                    }
                });
        }

        private IObservable<Unit> BuildStreamQuery(string tab, string where, IList<When> expressions, bool negate = false)
        {
            var filter = where;
            var whereBuilder = new StringBuilder(@"
var func = new Func<IEventStream, IObservable<Unit>>(stream =>
");

            for (int i = 0; i < expressions.Count; i++)
            {
                var var = ((char)(97 + i)).ToString(); // 97 == 'a'
                var when = expressions[i];
                var type = TopicCodeTypes.Find(topicRegistry.Find(when.Topic));
                if (type == null)
                    throw new ArgumentException(Strings.Brain.UnknonwnBehaviorTopic(when.Topic, where));

                whereBuilder.Append(tab).AppendLine("from {var} in stream.Impulses<{type}>(\"{topic}\", \"{devices}\")".FormatWith(
                    new { var = var, type = type, topic = when.Topic, devices = when.Devices }));

                filter = ReplaceFilteredTopicWithVariable(filter, var, when);
            }

            whereBuilder.Append(tab).Append("where ").Append(negate ? "!(" : "(")
                .Append(filter).AppendLine(")")
                .Append(tab).AppendLine("select Unit.Default")
                .AppendLine(");")
                .AppendLine()
                .Append("func;");

            var whereExpression = whereBuilder.ToString();
            Tracing.Brain.BuiltBehaviorQuery(where, whereExpression);

            var func = (Func<IEventStream, IObservable<Unit>>)evaluator.Evaluate(whereExpression);
            var query = func(stream);

            return query;
        }

        private IQueryable<Unit> BuildStateQuery(string tab, string where, IList<When> expressions)
        {
            var filter = where;
            var whereBuilder = new StringBuilder(@"
var func = new Func<ISystemState, IQueryable<Unit>>(state =>
");

            for (int i = 0; i < expressions.Count; i++)
            {
                var var = ((char)(97 + i)).ToString(); // 97 == 'a'
                var when = expressions[i];
                var type = TopicCodeTypes.Find(topicRegistry.Find(when.Topic));
                if (type == null)
                    throw new ArgumentException(Strings.Brain.UnknonwnBehaviorTopic(when.Topic, where));

                whereBuilder.Append(tab).AppendLine("from {var} in state.Of<{type}>(\"{topic}\", \"{devices}\")".FormatWith(
                    new { var = var, type = type, topic = when.Topic, devices = when.Devices }));

                filter = ReplaceFilteredTopicWithVariable(filter, var, when);
            }

            whereBuilder.Append(tab).Append("where ")
                .AppendLine(filter)
                .Append(tab).AppendLine("select Unit.Default")
                .AppendLine(");")
                .AppendLine()
                .Append("func;");

            var whereExpression = whereBuilder.ToString();
            Tracing.Brain.BuiltStateQuery(where, whereExpression);

            var func = (Func<ISystemState, IQueryable<Unit>>)evaluator.Evaluate(whereExpression);
            var query = func(state);

            return query;
        }

        private Action<CommandContext, IEventStream, ISystemState, IClock> BuildStreamAction(string tab, string action, IList<Then> expressions)
        {
            var thenBuilder = new StringBuilder(@"
var action = new Action<CommandContext, IEventStream, ISystemState, IClock>((ctx, stream, state, clock) =>
{
");
            foreach (var then in expressions)
            {
                var type = topicRegistry.Find(then.Topic);
                if (type == TopicType.Unknown)
                    throw new ArgumentException(Strings.Brain.UnknonwnBehaviorTopic(then.Topic, action));

                if (string.IsNullOrEmpty(then.Value) || type == TopicType.Void)
                {
                    thenBuilder.Append(tab).AppendLine("stream.Push(ctx, Command.Create(\"{topic}\", clock.Now, \"{devices}\"));".FormatWith(
                        new { topic = then.Topic, devices = then.Devices }));
                }
                else
                {
                    var value = then.Value;
                    if (type == TopicType.String)
                        value = "\"" + value + "\"";
                    else if (type == TopicType.Number)
                        value += "f";

                    thenBuilder.Append(tab).AppendLine("stream.Push(ctx, Command.Create(\"{topic}\", {value}, clock.Now, \"{devices}\"));".FormatWith(
                        new { topic = then.Topic, value = value, devices = then.Devices }));
                }
            }

            thenBuilder.AppendLine("});").AppendLine()
                .Append("action;");

            var thenExpression = thenBuilder.ToString();
            Tracing.Brain.BuiltBehaviorAction(action, thenExpression.Replace("{", "{{").Replace("}", "}}"));

            return (Action<CommandContext, IEventStream, ISystemState, IClock>)evaluator.Evaluate(thenExpression);
        }

        private static string ReplaceFilteredTopicWithVariable(string filter, string var, When when)
        {
            // TODO: this replacement is WAY too brittle!
            var match = when.Topic;
            if (!string.IsNullOrEmpty(when.Devices))
            {
                match += "(" + when.Devices + ")";
                filter = filter.Replace(match, var);
            }
            else
            {
                // TODO: not entirely safe, since the topic might have whitespace characters, etc... 
                filter = Regex.Replace(filter, @"\b" + match + @"\b", var);
            }
            return filter;
        }

        internal class BehaviorEntry
        {
            public BehaviorEntry()
            {
                UndoCommands = new List<ICommand>();
            }

            public string Behavior { get; set; }
            public bool IsActive { get; set; }
            public Statement Statement { get; set; }
            public IDisposable Enter { get; set; }
            public IDisposable Exit { get; set; }
            public List<ICommand> UndoCommands { get; set; }
        }

        internal class DeviceEntry
        {
            public IDevice Device { get; set; }
            public IDisposable Commands { get; set; }
            public IDisposable Impulses { get; set; }
        }
    }
}