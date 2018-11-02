using System.Threading;
using System.Collections.Concurrent;


namespace ExampleArgs.Network
{
    public static class IOArgsPool
    {
        public const int MaxDefaultArgsCount = 1024 * 1024;

        private static ConcurrentQueue<IOArgs> _argsPool = new ConcurrentQueue<IOArgs>();
        private static int _argsMaxCount = MaxDefaultArgsCount;

        public static int MaxArgsCount
        {
            get
            {
                return Thread.VolatileRead(ref _argsMaxCount);
            }
            set
            {
                Thread.VolatileWrite(ref _argsMaxCount, value);
            }
        }

        public static IOArgs Alloc(int size = -1)
        {
            if (_argsPool.TryDequeue(out IOArgs result))
            {
                if (size > 0 )
                {
                    result.EnsureLength(size);
                }

                return result;
            }

            return new IOArgs(size);
        }

        internal static void Return(IOArgs args)
        {
            if (_argsPool.Count > MaxArgsCount)
            {
                args.BaseDispose();
            }

            _argsPool.Enqueue(args);
        }
    }
}
