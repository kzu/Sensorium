namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Mono.Zeroconf;

    public class IpPort
    {
        public string Ip { get; set; }
        public int Port { get; set; }
    }

    public static class ServiceDiscovery
    {
        public static IpPort DiscoverBroker()
        {
            var host = default(IpPort);

            var browser = new ServiceBrowser();
            browser.ServiceAdded += (sender, e) =>
            {
                e.Service.Resolved += (sr, er) => host = new IpPort
                { 
                    Ip = er.Service.HostEntry.AddressList[0].ToString(),
                    Port = IPAddress.NetworkToHostOrder((short)er.Service.Port),
                };
                e.Service.Resolve();
            };
            browser.Browse(0, AddressProtocol.IPv4, Bonjour.ServiceType, Bonjour.ReplyDomain);

            Console.Write("Waiting for " + Bonjour.ServiceName + " to become available in the network.");

            while (host == null)
            {
                Console.Write(".");
                Thread.Sleep(100);
            }

            Console.WriteLine();
            Console.WriteLine("Found " + Bonjour.ServiceName + " at " + host.Ip + ":" + host.Port);

            return host;
        }
   }
}