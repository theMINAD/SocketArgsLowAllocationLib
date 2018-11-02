using System;
using System.Net.Sockets;
using ExampleArgs.Network;
using ExampleArgs.Network.Handlers;

namespace TestArgs.Handlers
{
    public class EchoConnection : TcpHandler
    {
        public static IOQueue ClientQueue { get; } = new IOQueue();
        public static bool LogReceive { get; set; }

        public EchoConnection(Socket socket = null) : base(ClientQueue, socket)
        {
        }

        protected override void OnReceive(IOArgs args)
        {
            //Log our received data:
            if (LogReceive)
            {
                Console.Write("RECV:");
                using (var output = Console.OpenStandardOutput())
                {
                    output.Write(args.OperationBuffer.Span);
                }
                Console.WriteLine();
            }

            //Echo received bytes back:
            args.SetBuffer(args.OperationBuffer);
            SendArgs(args);
        }

        protected override void OnConnect(IOArgs args)
        {
            Console.WriteLine($"Connected to { RemoteEndPoint.ToString()} ");

            //Sending string or Memory<byte> will automaticly create IOArgs with pooled buffer.
            //Send($"HELLO FROM { LocalEndPoint }");

            args.SetBuffer($"HELLO FROM { LocalEndPoint }"); //Copy string to our IOArg's buffer.
            SendArgs(args);
        }

        public void DoReceive(IOArgs args)
        {
            try
            {
                args.EnsureLength(2048); //Make sure we have space to receive on.
                args.SetBuffer(0, 2048); //Set range where want to receive.
                ReceiveArgs(args); //Reuse same args for recv operation.
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                args.Dispose(); //Dispose since error;
                Dispose(); //Dispose since exception.
            }
        }

        protected override void OnSend(IOArgs args)
        {
            DoReceive(args);
        }

        public override void OnIOException(IOArgs args, Exception ex)
        {
            //Print the operation where args throw error:
            Console.WriteLine($"Exception { ex.Message }, { args.LastOperation }");

            //Dispose args, and dispose.
            args.Dispose();
            Dispose();
        }
    }
}
