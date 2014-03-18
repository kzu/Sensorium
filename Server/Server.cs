namespace Sensorium
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Text;
    using ReactiveSockets;
    using Sprache;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reactive.Disposables;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using System.IO;
    using System.Threading.Tasks;

    class Server
    {
        private static readonly Parser<Tuple<string, TopicType>> TopicParser = from _ in Parse.String("topic").Token()
                                                                               from type in Parse.String("bool").Return(TopicType.Boolean).Or(
                                                                                            Parse.String("boolean").Return(TopicType.Boolean)).Or(
                                                                                            Parse.String("number").Return(TopicType.Number)).Or(
                                                                                            Parse.String("string").Return(TopicType.String)).Or(
                                                                                            Parse.String("void").Return(TopicType.Void)).Token()
                                                                               from topic in Expressions.Grammar.Topic
                                                                               select Tuple.Create(topic, type);

        private static readonly Parser<Tuple<string, List<string>>> DeviceParser = from _ in Parse.String("device").Token()
                                                                                   from device in Expressions.Grammar.Topic.Token()
                                                                                   from topics in Expressions.Grammar.Topic.Token().DelimitedBy(Parse.Char(',')).Optional()
                                                                                   select Tuple.Create(device, topics.GetOrElse(new string[0]).ToList());

        private static List<IDevice> connectedDevices = new List<IDevice>();
        private static readonly ITracer tracer = Tracer.Get<Server>();

        static void Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure();
            Tracer.Initialize(new TracerManager());

            AppDomain.CurrentDomain.UnhandledException += (s, e) => tracer.Error(e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (s, e) => tracer.Error(e.Exception);

            try
            {
                short port = 1055;
                if (args.Length > 0)
                    port = short.Parse(args[0]);

                ServiceRegistration.Start(port);

                var devices = new DeviceRegistry();
                var topics = new Dictionary<string, TopicType>();
                var stream = new EventStream();
                var state = new SystemState();
                var impulseStore = new FileImpulseStore("Store\\Impulses");
                var commandStore = new FileCommandStore("Store\\Commands");

                SetupTracing(stream);

                // Hook up event stream consumers that perform orthogonal operations.
                new ClockImpulses(Clock.Default).Connect(stream);
                new CommandToBytes().Connect(stream);
                new SensedToImpulse(Sensorium.Clock.Default, topics).Connect(stream);
                new SetSystemState(state).Connect(stream);
                // Hook up stores
                new StoreCommands(commandStore).Connect(stream);
                new StoreImpulses(impulseStore).Connect(stream);
                
                var brain = new Brain(stream, devices, topics, state, Clock.Default);

                if (File.Exists("Server.cfg"))
                {
                    var setup = Setup.Read("Server.cfg", File.ReadAllText("Server.cfg"));
                    Console.WriteLine("Applying configuration file:");
                    Console.WriteLine(setup.ToString(true));
                    foreach (var topic in setup.Topics)
                    {
                        topics[topic.Key] = topic.Value;
                    }
                    foreach (var device in setup.DeviceTypes)
                    {
                        devices.Register(device.Type, device.Commands.ToArray());
                    }
                    foreach (var behavior in setup.Behaviors)
                    {
                        brain.Behave(behavior);
                    }
                }

                var server = new ReactiveListener(port);
                server.Connections.Subscribe(socket =>
                {
                    Console.WriteLine("New socket connected {0}", socket.GetHashCode());

                    var binary = new BinaryChannel(socket);
                    var message = new MessageChannel(binary);
                    var device = new TcpDevice(brain, message, Clock.Default);

                    connectedDevices.Add(device);
                    device.Disconnected += (sender, e) => socket.Dispose();
                    socket.Disconnected += (sender, e) =>
                        {
                            Console.WriteLine("Socket disconnected {0}", sender.GetHashCode());
                            connectedDevices.Remove(device);
                            device.Dispose();
                        };
                    socket.Disposed += (sender, e) =>
                        {
                            Console.WriteLine("Socket disposed {0}", sender.GetHashCode());
                            connectedDevices.Remove(device);
                            device.Dispose();
                        };
                });

                server.Start();

                Console.WriteLine("Define topic:");
                Console.WriteLine("  topic [void|bool|number|string] [name]");
                Console.WriteLine("Define device:");
                Console.WriteLine("  device [type] [comma-separated list of topic commands the device can receive]");
                Console.WriteLine("Define behavior:");
                Console.WriteLine("  behave [when then expression]");

                Console.WriteLine("Press Enter to exit");

                string line = null;
                while ((line = Console.ReadLine()) != "")
                {
                    if (line == Environment.NewLine)
                        return;

                    if (line.StartsWith("topic"))
                    {
                        var topic = TopicParser.Parse(line);
                        topics[topic.Item1] = topic.Item2;
                        Console.WriteLine("Registered topic '{0}' of type {1}", topic.Item1, topic.Item2);
                    }
                    else if (line.StartsWith("device"))
                    {
                        var device = DeviceParser.Parse(line);
                        devices.Register(device.Item1, device.Item2.ToArray());
                        Console.WriteLine("Registered device type '{0}' to receive commands {1}", device.Item1, string.Join(", ", device.Item2.Select(s => "'" + s + "'")));
                    }
                    else if (line.StartsWith("behave "))
                    {
                        try
                        {
                            brain.Behave(line.Substring(7));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed: {0}", e);
            }
        }

        private static void SetupTracing(EventStream stream)
        {
            stream.Of<IEventPattern<IDevice, ISensed>>().Subscribe(x => tracer.Info("{0}: {1}", x.Sender, x.EventArgs));
            stream.Of<IEventPattern<IDevice, IImpulse>>().Subscribe(x => tracer.Info("{0}: {1}", x.Sender, x.EventArgs));
            stream.Of<IEventPattern<IDevice, IssuedCommand>>().Subscribe(x => tracer.Info("{0}: {1}", x.Sender, x.EventArgs));

            //stream.Of<ICommand<float>>().Subscribe(x => tracer.Info("Command: {0}", x));
            //stream.Of<ICommand<bool>>().Subscribe(x => tracer.Info("Command: {0}", x));
            //stream.Of<ICommand<string>>().Subscribe(x => tracer.Info("Command: {0}", x));
            //stream.Of<ICommand<Unit>>().Subscribe(x => tracer.Info("Command: {0}", x));
        }

        class TcpDevice : IDevice
        {
            public const int DefaultTimeoutSeconds = 60;
            public static int TimeoutSeconds { get; set; }

            private static readonly ITracer tracer = Tracer.Get<TcpDevice>();

            private IChannel<IMessage> channel;
            private IClock clock;

            private IDisposable subscription;
            private Subject<ISensed> impulses = new Subject<ISensed>();
            private SerialDisposable timeout = new SerialDisposable();
            private SerialDisposable window = new SerialDisposable();

            public event EventHandler Disconnected = (sender, args) => { };
            private Brain brain;

            static TcpDevice()
            {
                TimeoutSeconds = DefaultTimeoutSeconds;
            }

            public TcpDevice(Brain brain, IChannel<IMessage> channel, IClock clock)
            {
                this.brain = brain;
                this.channel = channel;
                this.clock = clock;

                // Use a single background subscription.
                var receiver = channel.Receiver
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .Publish()
                    .RefCount();

                subscription = new CompositeDisposable(
                    receiver.OfType<Connect>().Subscribe(OnConnected),
                    receiver.OfType<Disconnect>().Subscribe(OnDisconnected),
                    receiver.OfType<Topic>().Subscribe(OnTopic),
                    receiver.OfType<IMessage>().Subscribe(_ => RefreshTimeout()),
                    timeout, 
                    window
                );

                RefreshTimeout();

                tracer.Info("Created TCP device");
            }

            public string Id { get; set; }
            public string Type { get; set; }
            public IObservable<ISensed> Impulses { get { return impulses; } }

            public void Send(IDo command)
            {
                // TODO: error handling?
                tracer.Info("{0}: Sending command '{1}' to device.", Id, command.Topic);
                channel.SendAsync(new Topic(command.Topic, command.Payload));
            }

            public void Dispose()
            {
                subscription.Dispose();
            }

            public override string ToString()
            {
                return Id + "(" + Type + ")";
            }

            private void OnConnected(Connect connect)
            {
                tracer.Info("{0}: Connected (type '{1}').", connect.DeviceId, connect.DeviceType);
                Id = connect.DeviceId;
                Type = connect.DeviceType;
                // Can only connect once we have a device id and type.
                brain.Connect(this);
            }

            private void OnDisconnected(Disconnect disconnect)
            {
                tracer.Info("{0}: Requested disconnection.", Id);
                Disconnected(this, EventArgs.Empty);
            }

            private void OnTopic(Topic topic)
            {
                tracer.Info("{0}: Received impulse '{1}'.", Id, topic.Name);
                impulses.OnNext(new Sensed(topic.Name, topic.Payload ?? new byte[0]));
            }

            private void OnTimeout()
            {
                tracer.Warn("{0}: Timeout. Did not receive any messages in {1} seconds.", Id, TimeoutSeconds);
                Disconnected(this, EventArgs.Empty);
            }

            private void RefreshTimeout()
            {
                // Every time we get any kind of message, we reset the window.
                timeout.Disposable = clock.Tick
                    .Window(TimeSpan.FromSeconds(TimeoutSeconds))
                    .Subscribe(w => window.Disposable = w.Subscribe(_ => { }, OnTimeout));

                tracer.Verbose("{0}: Refreshed timeout.", Id);
            }
        }

        class DeviceRegistry : IDeviceRegistry
        {
            private ConcurrentDictionary<string, List<string>> devices = new ConcurrentDictionary<string, List<string>>();

            public IEnumerable<string> GetCommands(string deviceType)
            {
                return devices.GetOrAdd(deviceType, _ => new List<string>());
            }

            public void Register(string deviceType, string[] commands)
            {
                devices.GetOrAdd(deviceType, _ => new List<string>()).AddRange(commands);
            }
        }
    }
}
