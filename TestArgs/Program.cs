using System;
using System.Net;
using System.Threading;
using TestArgs.Handlers;

namespace TestArgs
{
    class Program
    {
        static void Main(string[] args)
        {
            //Enable or disable log:
            EchoConnection.LogReceive = true;

            //Start the Queues:
            EchoServer.ServerQueue.Start();
            EchoConnection.ClientQueue.Start();

            //Address:
            IPEndPoint address = new IPEndPoint(IPAddress.Loopback, 5588);

            //Start listening
            EchoServer server = new EchoServer();
            server.Bind(address);

            //Start 10 accept operations:
            for (int i = 0; i < 10; i++)
            {
                server.Accept();
            }

            //Start client when S is hit.
            Console.WriteLine("HIT S to start client!");
            while (Console.ReadKey(true).Key != ConsoleKey.S) continue;

            //Connect 1000 connections:
            for (int i = 0; i < 1000; i++)
            {
                EchoConnection connection = new EchoConnection();
                connection.Connect(address);
            }

            //Close app when Q is hit
            Console.WriteLine("HIT Q to close app.");
            while (Console.ReadKey(true).Key != ConsoleKey.Q) continue;
        }
    }
}
