namespace Sensorium
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading.Tasks;
    using ReactiveSockets;

    /// <summary>
    /// Implements a communication channel over a socket that 
    /// has a fixed length header and a variable length string 
    /// payload.
    /// </summary>
    public class BinaryChannel : IChannel<byte[]>
    {
        private const byte MaxLength = byte.MaxValue - 1;
        private IReactiveSocket socket;

        /// <summary>
        /// Initializes the channel with the given socket.
        /// </summary>
        public BinaryChannel(IReactiveSocket socket)
        {
            this.socket = socket;

            Receiver = from length in socket.Receiver
                       let body = socket.Receiver.Take(length)
                       select body.ToEnumerable().ToArray();
        }

        /// <summary>
        /// Stream of bytes received through the channel.
        /// </summary>
        public IObservable<byte[]> Receiver { get; private set; }

        /// <summary>
        /// Asynchronously sends a binary payload through the channel.
        /// </summary>
        public Task SendAsync(byte[] message)
        {
            return socket.SendAsync(Convert(message));
        }

        /// <summary>
        /// Converts the body to a message payload that contains 
        /// an integer prefix with the payload length.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        internal byte[] Convert(byte[] body)
        {
            if (body.Length > MaxLength)
                throw new ArgumentOutOfRangeException("Message can't be more than 254 bytes long");

            var header = (byte)body.Length;
            var payload = new byte[] { header }.Concat(body).ToArray();

            return payload;
        }
    }
}
