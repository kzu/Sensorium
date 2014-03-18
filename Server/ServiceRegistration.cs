namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using Mono.Zeroconf;

    public static class ServiceRegistration
    {
        private static RegisterService service;

        public static void Start(short port)
        {
            service = new RegisterService
            {
                Name = Bonjour.ServiceName,
                RegType = Bonjour.ServiceType,
                // NOTE: port may have been dynamically determined by the broker
                Port = port,
                UPort = (ushort)port,
                ReplyDomain = Bonjour.ReplyDomain,
                //TxtRecord = new TxtRecord
                //{
                //    { 
                //        Bonjour.IpRecord, 
                //        Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                //        .ToString() 
                //    }, 
                //    {
                //        Bonjour.PortRecord,
                //        port.ToString()
                //    }
                //}
            };

            service.Response += OnRegisterServiceResponse;
            service.Register();
        }

        private static void OnRegisterServiceResponse(object o, RegisterServiceEventArgs args)
        {
            switch (args.ServiceError)
            {
                case ServiceErrorCode.NameConflict:
                    Console.WriteLine("*** Name Collision! '{0}' is already registered",
                        args.Service.Name);
                    break;
                case ServiceErrorCode.None:
                    Console.WriteLine("*** Registered name = '{0}'", args.Service.Name);
                    break;
                case ServiceErrorCode.Unknown:
                    Console.WriteLine("*** Error registering name = '{0}'", args.Service.Name);
                    break;
            }
        }
    }
}