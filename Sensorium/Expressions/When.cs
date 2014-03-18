namespace Sensorium.Expressions
{
    public class When
    {
        public string Topic { get; set; }
        public string Devices { get; set; }
        public Comparison Comparison { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            var value = Topic;
            if (!string.IsNullOrEmpty(Devices))
                value += "(" + Devices + ")";

            switch (Comparison)
            {
                case Comparison.GreaterThan:
                    value += " > ";
                    break;
                case Comparison.LessThan:
                    value += " < ";
                    break;
                case Comparison.Equal:
                    value += " == ";
                    break;
                case Comparison.NotEqual:
                    value += " != ";
                    break;
                default:
                    break;
            }

            return value + Value;
        }
    }
}