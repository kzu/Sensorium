namespace Sensorium.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

    public static class Grammar
    {
        public static readonly Parser<char> NewLineSymbol = Parse.Char('\r').Or(Parse.Char('\n'));

        // when
        public static readonly Parser<string> WhenSymbol = Parse.String("when").Text().Token();
        // operator
        public static readonly Parser<Operator> WhenOperatorType = Parse.String("&&").Token().Return(Operator.And).Or(
                                                                   Parse.String("||").Token().Return(Operator.Or));
        // then
        public static readonly Parser<string> ThenSymbol = Parse.String("then").Text().Token();
        // operator
        public static readonly Parser<Operator> ThenOperatorType = Parse.String("&&").Token().Return(Operator.And);

        // quoted text
        public static readonly Parser<string> QuotedText = (from open in Parse.Char('"')
                                                            from content in Parse.CharExcept('"').Many().Text()
                                                            from close in Parse.Char('"')
                                                            select content);
        // topic
        public static readonly Parser<string> BareTopic = Parse.LetterOrDigit.Or(Parse.Char('/')).Or(Parse.Char('_')).AtLeastOnce().Text();
        public static readonly Parser<string> Topic = QuotedText.Or(BareTopic);

        // devices
        public static readonly Parser<IOption<string>> Devices = (from open in Parse.Char('(')
                                                                  from devices in Parse.CharExcept(')').Many().Text()
                                                                  from close in Parse.Char(')')
                                                                  select devices).Optional();
        // comparison
        public static readonly Parser<Comparison> ComparisonType = Parse.Char('<').Return(Comparison.LessThan).Or(
                                                                   Parse.Char('>').Return(Comparison.GreaterThan)).Or(
                                                                   Parse.String("==").Return(Comparison.Equal)).Or(
                                                                   // This is such a common typing mistake, we'll fix it
                                                                   Parse.String("=").Return(Comparison.Equal)).Or(
                                                                   Parse.String("!=").Return(Comparison.NotEqual)).Token();
        // value
        public static readonly Parser<string> Value = Parse.CharExcept(new [] {' ', '&', '\r', '\n' }).Many().Text();

        // when [topic] [comparison] [value]
        public static readonly Parser<IEnumerable<When>> When = (from t in Grammar.Topic.Token()
                                                                 from d in Grammar.Devices
                                                                 from op in Grammar.ComparisonType
                                                                 from v in Grammar.QuotedText.Or(Grammar.Value)
                                                                 select new When { Topic = t, Devices = d.GetOrDefault(), Comparison = op, Value = v })
                                                                .DelimitedBy(Grammar.WhenOperatorType)
                                                                .Named("when");

        // then [topic] [value]
        public static readonly Parser<IEnumerable<Then>> Then = (from t in Grammar.Topic.Token()
                                                                 from d in Grammar.Devices
                                                                 from v in Grammar.ThenValue.Optional()
                                                                 select new Then { Topic = t, Devices = d.GetOrDefault(), Value = v.GetOrDefault() })
                                                                .DelimitedBy(Grammar.ThenOperatorType)
                                                                 .Named("then");

        // = [value]
        public static readonly Parser<string> ThenValue = (from _ in Parse.Char('=').Token()
                                                           from v in Grammar.QuotedText.Or(Grammar.Value)
                                                           select v == "" ? default(string) : v)
                                                           .Named("value");

        // when [topic] [comparison] [value] then [topic] [value]
        public static readonly Parser<Statement> Statement = (from _ in WhenSymbol
                                                              from when in When
                                                              from __ in ThenSymbol
                                                              from then in Then
                                                              select new Statement { When = when.ToList(), Then = then.ToList() }).Named("statement");
    }
}