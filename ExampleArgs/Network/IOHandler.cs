using System;
using System.Net.Sockets;

namespace ExampleArgs.Network
{
    public abstract class IOHandler
    {
        public abstract void OnIO(IOArgs args);
        public abstract void OnIOException(IOArgs args, Exception ex);

        public IOQueue Queue { get; }

        public IOHandler(IOQueue queue)
        {
            Queue = queue;
        }

        public void SubmitIO(IOArgs args)
        {
            Queue.Submit(args);
        }
    }
}
