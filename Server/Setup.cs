namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sensorium.Expressions;
    using Sprache;

    public class Setup
    {
        private List<Statement> parsedStatements;

        public string FileName { get; set; }
        public IDictionary<string, TopicType> Topics { get; set; }
        public IList<string> Behaviors { get; set; }
        public IList<DeviceInfo> DeviceTypes { get; set; }

        public List<Statement> ParsedStatements
        {
            get
            {
                return parsedStatements ?? (parsedStatements =
                    Behaviors.Select(s => Grammar.Statement.Parse(s)).ToList());
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
                string.Join(Environment.NewLine, Behaviors.Select(s => tab + s)) +
                Environment.NewLine + "devices" + Environment.NewLine +
                string.Join(Environment.NewLine, DeviceTypes.Select(g => tab + g));
        }

        public static Setup Read(string fileName, string fileContents)
        {
            try
            {
                var setup = Parser.SetupExpression.Parse(fileContents);
                setup.FileName = Path.GetFileNameWithoutExtension(fileName);
                return setup;

            }
            catch (ParseException e)
            {
                throw new ParseException("Failed to parse file " + fileName, e);
            }
        }

        private static class Parser
        {
            internal static readonly Parser<Tuple<string, TopicType>> TopicDefinitionExpression = from type in Parse.String("bool").Return(TopicType.Boolean).Or(
                                                                                           Parse.String("boolean").Return(TopicType.Boolean)).Or(
                                                                                           Parse.String("number").Return(TopicType.Number)).Or(
                                                                                           Parse.String("string").Return(TopicType.String)).Or(
                                                                                           Parse.String("void").Return(TopicType.Void)).Token()
                                                                                                  from topic in Expressions.Grammar.Topic
                                                                                                  select Tuple.Create(topic, type);

            internal static readonly Parser<string> BehaviorExpression = from _ in Parse.String("when").Token()
                                                                         from w in Parse.AnyChar.Except(Parse.String("then")).Many().Text()
                                                                         from __ in Parse.String("then").Token()
                                                                         from t in Parse.CharExcept(new[] { '\r', '\n' }).Many().Text()
                                                                         select "when " + w + " then " + t;

            internal static readonly Parser<IOption<IEnumerable<string>>> DeviceTopicsExpression = (from open in Parse.Char('(')
                                                                                                    from topics in Grammar.Topic.Token().DelimitedBy(Parse.Char(','))
                                                                                                    from close in Parse.Char(')')
                                                                                                    select topics)
                                                                                         .Optional();

            internal static readonly Parser<IEnumerable<string>> DeviceInExpression = from _ in Parse.String("in:").Token()
                                                                                      from topics in Grammar.Topic.Token().DelimitedBy(Parse.Char(','))
                                                                                      select topics;
            internal static readonly Parser<IEnumerable<string>> DeviceOutExpression = from _ in Parse.String("out:").Token()
                                                                                       from topics in Grammar.Topic.Token().DelimitedBy(Parse.Char(','))
                                                                                       select topics;

            internal static readonly Parser<DeviceInfo> DeviceExpression = from deviceType in Parse.LetterOrDigit.AtLeastOnce().Text()
                                                                           from open in Parse.Char('(')
                                                                           from commands in DeviceInExpression.Optional()
                                                                           from colon in Parse.Char(';').Optional()
                                                                           from impulses in DeviceOutExpression.Optional()
                                                                           select new DeviceInfo 
                                                                           { 
                                                                               Type = deviceType, 
                                                                               Commands = commands.GetOrElse(new string[0]).ToList(),
                                                                               Impulses = impulses.GetOrElse(new string[0]).ToList(), 
                                                                           };

            internal static readonly Parser<DeviceInfo> DeviceExpression2 = from deviceType in Parse.LetterOrDigit.AtLeastOnce().Text()
                                                                            from deviceTopics in DeviceTopicsExpression
                                                                            select new DeviceInfo { Type = deviceType, Commands = deviceTopics.GetOrElse(new string[0]).ToList() };

            internal static readonly Parser<Setup> SetupExpression = from _ in Parse.String("setup").Token()
                                                                     from topics in TopicDefinitionExpression.Many()
                                                                     from statements in BehaviorExpression.Many()
                                                                     from __ in Parse.String("devices").Token()
                                                                     from device in DeviceExpression.Many()
                                                                     select new Setup
                                                                     {
                                                                         Topics = topics.ToDictionary(t => t.Item1, t => t.Item2),
                                                                         Behaviors = statements.ToList(),
                                                                         DeviceTypes = device.ToList(),
                                                                     };
        }
    }

    public class DeviceInfo
    {
        public string Type { get; set; }
        public IList<string> Commands { get; set; }
        public IList<string> Impulses { get; set; }

        public override string ToString()
        {
            return Type +
                "(in: " + string.Join(", ", Commands) + 
                "; out: " + string.Join(", ", Impulses) + ")";
        }
    }

}