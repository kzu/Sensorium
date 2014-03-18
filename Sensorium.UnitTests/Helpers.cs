namespace Sensorium
{
    using System;
    using System.Linq;

    internal static class Helpers
    {
        public static string ToPhrase(this string text)
        {
            return new String(text.SelectMany((c, i) =>
                i != 0 && char.IsUpper(c) && !char.IsUpper(text[i - 1]) ? new char[] { ' ', c } : new char[] { c })
                .ToArray());
        }
    }
}