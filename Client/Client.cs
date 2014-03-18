namespace Sensorium
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using ReactiveSockets;

    class Client
    {
        static void Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure();
            Tracer.Initialize(new TracerManager());
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Tracer.Get<Client>().Error(e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (s, e) => Tracer.Get<Client>().Error(e.Exception);

            try
            {
                var ipPort = ServiceDiscovery.DiscoverBroker();
                var host = ipPort.Ip;
                var port = ipPort.Port;

                //var host = "localhost";
                //var port = 1055;

                //if (args.Length > 0)
                //    host = args[0];
                //if (args.Length > 1)
                //    port = int.Parse(args[1]);

                var client = new ReactiveClient(host, port);
                var binary = new BinaryChannel(client);
                var channel = new MessageChannel(binary);

                channel.Receiver.SubscribeOn(TaskPoolScheduler.Default).Subscribe(
                    s => 
                    {
                        Console.WriteLine("Received: {0}", s);
                        // Report state change immediately.
                        var topic = s as Topic;
                        if (topic != null)
                        {
                            Console.WriteLine("Sending back to cause state change: {0}", topic);
                            channel.SendAsync(topic);
                        }
                    },
                    e => Console.WriteLine(e),
                    () => Console.WriteLine("Socket receiver completed"));


                Console.WriteLine("To connect, enter: connect [device id] [device type]");
                Console.WriteLine("To send message once connected: [topic] = [value], where [value] can be:");
                Console.WriteLine("  * a boolean (words 'true' or 'false' without quotes)");
                Console.WriteLine("  * a number (a numberic value followed by the suffic 'f' denoting a floating point number)");
                Console.WriteLine("  * a string (without quotes)");
                Console.WriteLine("If no value is provided after the topic, then it's assumed to be a void topic (no payload needed)");

                string line = null;
                while ((line = Console.ReadLine()) != "")
                {
                    if (line.StartsWith("connect "))
                    {
                        var connectInfo = line.Substring(8).Split(' ');
                        var deviceId = connectInfo[0];
                        var deviceType = connectInfo[1];

                        Console.WriteLine("Connecting...");
                        try
                        {
                            client.ConnectAsync().Wait();
                            channel.SendAsync(new Connect(deviceId, deviceType)).Wait();
                            Console.WriteLine("Connected!");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to connect: {0}", e);
                        }
                    }
                    else if (line == "disconnect")
                    {
                        if (client.IsConnected)
                        {
                            Console.WriteLine("Disconnecting...");
                            try
                            {
                                channel.SendAsync(new Disconnect()).Wait();
                                client.Disconnect();
                                Console.WriteLine("Disconnected!");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to disconnect: {0}", e);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Client was already disconnected.");
                        }
                    }
                    else if (line == "r")
                    {
                        Console.WriteLine("Reconnecting...");
                        client.Disconnect();
                        client.ConnectAsync().Wait();
                        Console.WriteLine("IsConnected = {0}. Re-send Connect message.", client.IsConnected);
                    }
                    else
                    {
                        Console.WriteLine("Sending...");
                        if (line.IndexOf('=') == -1)
                        {
                            channel.SendAsync(new Topic(line.Trim()));
                        }
                        else
                        {
                            var parts = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim()).ToArray();
                            if (parts[1].Equals("true", StringComparison.OrdinalIgnoreCase))
                                channel.SendAsync(new Topic(parts[0], Payload.ToBytes(true)));
                            else if (parts[1].Equals("false", StringComparison.OrdinalIgnoreCase))
                                channel.SendAsync(new Topic(parts[0], Payload.ToBytes(false)));
                            else if (parts[1].EndsWith("f"))
                                channel.SendAsync(new Topic(parts[0], Payload.ToBytes(float.Parse(parts[1].Substring(0, parts[1].Length - 1)))));
                            else
                                channel.SendAsync(new Topic(parts[0], Payload.ToBytes(parts[1])));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed: {0}", e);
            }
        }
    }
}
