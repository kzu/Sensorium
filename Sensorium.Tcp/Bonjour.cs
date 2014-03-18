namespace Sensorium
{
    using System;
    using System.Linq;

    public static class Bonjour
    {
        public const string ServiceName = "Sensorium Broker";
        public const string ServiceType = "_sensorium._tcp";
        public const string ReplyDomain = "local.";
        public const string IpRecord = "ip";
        public const string PortRecord = "port";
    }
}