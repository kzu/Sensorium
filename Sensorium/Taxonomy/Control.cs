namespace Sensorium.Taxonomy
{
    public static class Control
    {
        public const string On = "control/{device}/on";
        public const string Off = "control/{device}/off";
        public const string Up = "control/{device}/up";
        public const string Down = "control/{device}/down";
        public const string Left = "control/{device}/left";
        public const string Right = "control/{device}/right";

        public const string Mute = "control/{device}/mute";
        public const string VolumeUp = "control/{device}/vol-up";
        public const string VolumeDown = "control/{device}/vol-down";
        public const string ChannelUp = "control/{device}/ch-up";
        public const string ChannelDown = "control/{device}/ch-down";
    }
}
