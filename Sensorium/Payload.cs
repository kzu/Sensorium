namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Text;

    public static class Payload
    {
        public static bool ToBoolean(byte[] payload)
        {
            return BitConverter.ToBoolean(payload, 0);
        }

        public static float ToNumber(byte[] payload)
        {
            return BitConverter.ToSingle(payload, 0);
        }

        public static string ToString(byte[] payload)
        {
            return Encoding.UTF8.GetString(payload);
        }

        public static byte[] ToBytes(bool payload)
        {
            return BitConverter.GetBytes(payload);
        }

        public static byte[] ToBytes(float payload)
        {
            return BitConverter.GetBytes(payload);
        }

        public static byte[] ToBytes(string payload)
        {
            return Encoding.UTF8.GetBytes(payload);
        }
    }
}