namespace Sensorium.Expressions
{
    using System.Collections.Generic;
    using System.Linq;

    public class Statement
    {
        public IList<When> When { get; set; }
        public IList<Then> Then { get; set; }

        public override string ToString()
        {
            return "when " + string.Join(" ", When.Select(w => w.ToString())) +
                " then " + string.Join(" ", Then.Select(t => t.ToString()));
        }
    }
}