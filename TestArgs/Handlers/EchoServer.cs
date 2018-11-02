using System;
using ExampleArgs.Network;
using ExampleArgs.Network.Handlers;

namespace TestArgs.Handlers
{
    public class EchoServer : TcpHandler
    {
        public static IOQueue ServerQueue { get; } = new IOQueue();

        public EchoServer() : base(ServerQueue)
        {
        }

        protected override void OnAccept(IOArgs args)
        {
            EchoConnection connection = new EchoConnection(args.AcceptSocket);

            try
            {
                IOArgs receiveArgs = IOArgs.Alloc(); //Allocate args

                connection.DoReceive(receiveArgs);
            }
            catch (Exception)
            {
                connection.Dispose();
            }

            //AcceptSocket, must be null/non connected socket.
            args.AcceptSocket = null; 

            Accept(args);
        }

        public override void OnIOException(IOArgs args, Exception ex)
        {
            args.Dispose();
            Dispose();
        }
    }
}
