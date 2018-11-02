using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ExampleArgs.Network
{
    public class IOQueue : IDisposable
    {
        public enum IOQueueState : int
        {
            Stopped,
            Running,
            Disposed,
        }

        private readonly BlockingCollection<IOArgs> _pendingArgs;
        private readonly CancellationTokenSource _cancelTokenSource;
        private int _state;

        public IOQueueState State
        {
            get
            {
                return (IOQueueState)Thread.VolatileRead(ref _state);
            }
        }
        public CancellationToken CancelToken
        {
            get
            {
                return _cancelTokenSource.Token;
            }
        }

        public IOQueue()
        {
            _pendingArgs = new BlockingCollection<IOArgs>();
            _cancelTokenSource = new CancellationTokenSource();
            _state = (int)IOQueueState.Stopped;
        }

        public void Submit(IOArgs args)
        {
            _pendingArgs.Add(args);
        }

        private void Run()
        {
            while (_pendingArgs.TryTake(out IOArgs args, Timeout.Infinite, CancelToken))
            {
                if (args.Handler == null)
                {
                    args.Dispose();
                }
                else
                {
                    try
                    {
                        if (args.SocketError != SocketError.Success)
                        {
                            throw new SocketException((int)args.SocketError);
                        }

                        if (args.LastOperation == SocketAsyncOperation.Receive)
                        {
                            if (args.BytesTransferred == 0)
                            {
                                throw new SocketException((int)SocketError.ConnectionReset);
                            }
                        }

                        args.Handler.OnIO(args);
                    }
                    catch (Exception ex)
                    {
                        args.Handler.OnIOException(args, ex);
                    }
                }
            }

            if (State == IOQueueState.Disposed)
            {
                while (_pendingArgs.TryTake(out IOArgs args))
                {
                    if (args.Handler == null)
                    {
                        args.Dispose();
                    }
                    else
                    {
                        args.Handler.OnIOException(args, new ObjectDisposedException(""));
                    }
                }

                _pendingArgs.Dispose();
                _cancelTokenSource.Dispose();
            }
        }

        public void Start()
        {
            IOQueueState oldState;

            oldState = (IOQueueState)Interlocked.CompareExchange(ref _state, (int)IOQueueState.Running, (int)IOQueueState.Stopped);
            switch (oldState)
            {
                case IOQueueState.Disposed:
                    throw new ObjectDisposedException("");
                case IOQueueState.Running:
                    throw new OperationCanceledException();
            }

            Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            IOQueueState oldState;

            oldState = (IOQueueState)Interlocked.CompareExchange(ref _state, (int)IOQueueState.Stopped, (int)IOQueueState.Running);
            switch (oldState)
            {
                case IOQueueState.Disposed:
                    throw new ObjectDisposedException("");
                case IOQueueState.Stopped:
                    throw new OperationCanceledException();
            }

            _cancelTokenSource.Cancel();
        }

        public void Dispose()
        {
            IOQueueState oldState;

            oldState = (IOQueueState)Interlocked.Exchange(ref _state, (int)IOQueueState.Disposed);
            switch (oldState)
            {
                case IOQueueState.Disposed:
                    break;

                case IOQueueState.Running:
                    _cancelTokenSource.Cancel();
                    _pendingArgs.CompleteAdding();
                    break;

                case IOQueueState.Stopped:
                    _pendingArgs.Dispose();
                    _cancelTokenSource.Dispose();
                    break;
            }
        }
    }
}
