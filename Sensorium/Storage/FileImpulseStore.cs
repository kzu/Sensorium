namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using Newtonsoft.Json;

    public class FileImpulseStore : IImpulseStore
    {
        private string targetPath;
        private JsonSerializer serializer;

        public FileImpulseStore(string targetPath)
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

        public void Save(IDevice device, IImpulse impulse)
        {
            var path = Path.Combine(targetPath, impulse.Timestamp.Year.ToString(), impulse.Timestamp.Month.ToString("d2"), impulse.Timestamp.Day.ToString("d2"));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, new ImpulseEntry
                {
                    DeviceId = device.Id, 
                    DeviceType = device.Type,
                    Timestamp = impulse.Timestamp,
                    Topic = impulse.Topic,
                    Payload = ((dynamic)impulse).Payload,
                });

                File.WriteAllText(Path.Combine(path, impulse.Timestamp.UtcTicks + ".txt"), writer.ToString());
            }
        }

        public IEnumerable<IEventPattern<IDevice, IImpulse>> ReadAll()
        {
            return from year in Directory.EnumerateDirectories(targetPath).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from month in Directory.EnumerateDirectories(year).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from day in Directory.EnumerateDirectories(month).OrderBy(s => int.Parse(new DirectoryInfo(s).Name))
                   from file in Directory.EnumerateFiles(day).OrderBy(s => long.Parse(Path.GetFileNameWithoutExtension(s)))
                   select Read(file);
        }

        private IEventPattern<IDevice, IImpulse> Read(string file)
        {
            using (var fs = File.OpenRead(file))
            using (var sr = new StreamReader(fs))
            using (var json = new JsonTextReader(sr))
            {
                var entry = serializer.Deserialize<ImpulseEntry>(json);
                // Special case for number, since json will deserialize it as a double rather than single
                var impulse = default(IImpulse);
                if (entry.Payload is double)
                    impulse = Impulse.Create(entry.Topic, Convert.ToSingle((double)entry.Payload), entry.Timestamp);
                else
                    impulse = Impulse.Create(entry.Topic, entry.Payload, entry.Timestamp);

                var device = new DeviceInfo(entry.DeviceId, entry.DeviceType);
                
                return new EventPattern<IDevice, IImpulse>(device, impulse);
            }
        }

        public class ImpulseEntry
        {
            public string DeviceId { get; set; }
            public string DeviceType { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string Topic { get; set; }
            public dynamic Payload { get; set; }
        }
    }
}