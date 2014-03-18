namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using Newtonsoft.Json;

    public class FileCommandStore : ICommandStore
    {
        private string targetPath;
        private JsonSerializer serializer;

        public FileCommandStore(string targetPath)
        {
            this.targetPath = targetPath;

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

#if DEBUG
            this.serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented, TypeNameHandling = TypeNameHandling.Objects });
#else
            this.serializer = JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
#endif
        }

        public void Save(IDevice device, IssuedCommand issued)
        {
            var path = Path.Combine(targetPath, issued.Command.Timestamp.Year.ToString(), issued.Command.Timestamp.Month.ToString("d2"), issued.Command.Timestamp.Day.ToString("d2"));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, new CommandEntry
                {
                    DeviceId = device.Id, 
                    DeviceType = device.Type,
                    Timestamp = issued.Command.Timestamp,
                    Reason = issued.Reason,
                    Topic = issued.Command.Topic,
                    Payload = ((dynamic)issued.Command).Payload,
                });

                File.WriteAllText(Path.Combine(path, issued.Command.Timestamp.UtcTicks + ".txt"), writer.ToString());
            }
        }

        public IEnumerable<IEventPattern<IDevice, ICommand>> ReadAll()
        {
            return from year in Directory.EnumerateDirectories(targetPath).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from month in Directory.EnumerateDirectories(year).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from day in Directory.EnumerateDirectories(month).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from file in Directory.EnumerateFiles(day).OrderBy(s => long.Parse(Path.GetFileNameWithoutExtension(s)))
                   select Read(file);
        }

        private IEventPattern<IDevice, ICommand> Read(string file)
        {
            using (var fs = File.OpenRead(file))
            using (var sr = new StreamReader(fs))
            using (var json = new JsonTextReader(sr))
            {
                var entry = serializer.Deserialize<CommandEntry>(json);
                // Special case for number, since json will deserialize it as a double rather than single
                var command = default(ICommand);
                if (entry.Payload is double)
                    command = Command.Create(entry.Topic, Convert.ToSingle((double)entry.Payload), entry.Timestamp, entry.DeviceId);
                else
                    command = Command.Create(entry.Topic, entry.Payload, entry.Timestamp, entry.DeviceId);

                var device = new DeviceInfo(entry.DeviceId, entry.DeviceType);
                
                return new EventPattern<IDevice, ICommand>(device, command);
            }
        }

        public class CommandEntry
        {
            public string DeviceId { get; set; }
            public string DeviceType { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string Reason { get; set; }
            public string Topic { get; set; }
            public dynamic Payload { get; set; }
        }
    }
}