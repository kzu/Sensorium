namespace Sensorium.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    //using ProtoBuf;
    using Xunit;

    public class ProtoGen
    {
        [Fact(Skip = "No longer using protobuf")]
        public void when_generating_proto_then_succeeds()
        {
            var builder = new StringBuilder();

            //builder.AppendLine(Serializer.GetProto<Connect>());
            //builder.AppendLine(Serializer.GetProto<Disconnect>());
            //builder.AppendLine(Serializer.GetProto<Ping>());

            //builder.AppendLine(Serializer.GetProto<Topic>());

            var output = "package Sensorium;\r\n" + builder
                .ToString()
                .Replace("package Sensorium;", "")
                .Replace("\r\n\r\n", "\r\n");

            File.WriteAllText("..\\..\\..\\Sensorium.Tcp\\Messages.proto", output);
        }
    }
}