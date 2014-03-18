namespace Sensorium.Expressions
{
    public class Then
    {
        public string Topic { get; set; }
        public string Devices { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            var value = Topic;
            if (!string.IsNullOrEmpty(Devices))
                value += "(" + Devices + ")";
            if (!string.IsNullOrEmpty(Value))
                value += " = " + Value;

            return value;
        }
    }
}