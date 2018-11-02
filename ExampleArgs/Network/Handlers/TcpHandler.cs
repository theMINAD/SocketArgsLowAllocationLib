using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ExampleArgs.Network.Handlers
{
    public abstract class TcpHandler : IOHandler, IDisposable
    {
        private readonly Socket _socket;
        private int _disposed;

        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;
        public EndPoint LocalEndPoint => _socket.LocalEndPoint;
        public bool Connected => _socket.Connected;

        public bool Disposed
        {
            get
            {
                return Thread.VolatileRead(ref _disposed) == 1;
            }
        }

        public TcpHandler(IOQueue queue, Socket socket = null) : base(queue)
        {
            if (socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                _socket = socket;
            }
        }

        public void Disconnect(IOArgs args = null)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("");
            }

            if (!Connected)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            _socket.Shutdown(SocketShutdown.Both);

            IOArgs diconnectArgs = args == null ? IOArgs.Alloc(this) : args;
            diconnectArgs.DisconnectReuseSocket = true;
            diconnectArgs.Handler = this;

            try
            {
                if (!_socket.DisconnectAsync(diconnectArgs))
                {
                    SubmitIO(diconnectArgs);
                }
            }
            catch (Exception ex)
            {
                if (args == null)
                {
                    diconnectArgs.Dispose();
                }

                throw ex;
            }
        }

        public void Connect(EndPoint ep, IOArgs args = null)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("");
            }

            if (Connected)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            IOArgs connectArgs = args == null ? IOArgs.Alloc() : args;
            connectArgs.RemoteEndPoint = ep;
            connectArgs.Handler = this;

            try
            {
                if (!_socket.ConnectAsync(connectArgs))
                {
                    SubmitIO(connectArgs);
                }
            }
            catch (Exception ex)
            {
                if (args == null)
                {
                    connectArgs.Dispose();
                }
                
                throw ex;
            }
        }

        public void Accept(IOArgs args = null)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("");
            }

            IOArgs acceptArgs = args = args == null ? IOArgs.Alloc() : args;
            acceptArgs.Handler = this;

            try
            {
                if (!_socket.AcceptAsync(acceptArgs))
                {
                    SubmitIO(acceptArgs);
                }
            }
            catch (Exception ex)
            {
                if (args == null)
                {
                    acceptArgs.Dispose();
                }

                throw ex;
            }
        }

        public void Shutdown()
        {
            _socket.Shutdown(SocketShutdown.Both);
        }

        public void Bind(EndPoint ep, int backlog = sbyte.MaxValue)
        {
            _socket.Bind(ep);
            _socket.Listen(backlog);
        }

        public void ReceiveArgs(IOArgs args)
        {
            args.Handler = this;

            if (!_socket.ReceiveAsync(args))
            {
                SubmitIO(args);
            }
        }

        public void SendArgs(IOArgs args)
        {
            args.Handler = this;

            if (!_socket.SendAsync(args))
            {
                SubmitIO(args);
            }
        }

        public void Send(Memory<byte> buffer)
        {
            IOArgs args = IOArgs.Alloc();
            args.SetBuffer(buffer);

            try
            {
                if (!_socket.SendAsync(args))
                {
                    SubmitIO(args);
                }
            }
            catch (Exception ex)
            {
                args.Dispose();

                throw ex;
            }
        }

        public void Send(string str)
        {
            IOArgs args = IOArgs.Alloc();
            args.SetBuffer(str);

            try
            {
                if (!_socket.SendAsync(args))
                {
                    SubmitIO(args);
                }
            }
            catch (Exception ex)
            {
                args.Dispose();

                throw ex;
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 1) == 0)
            {
                _socket.Dispose();

                try
                {
                    OnDispose();
                }
                catch (Exception) { }
            }
        }

        public override void OnIO(IOArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    OnAccept(args);
                    break;
                case SocketAsyncOperation.Connect:
                    OnConnect(args);
                    break;
                case SocketAsyncOperation.Disconnect:
                    OnDisconnect(args);
                    break;
                case SocketAsyncOperation.Receive:
                    OnReceive(args);
                    break;
                case SocketAsyncOperation.Send:
                    OnSend(args);
                    break;
            }
        }

        protected virtual void OnDispose() { }
        protected virtual void OnAccept(IOArgs args) { }
        protected virtual void OnConnect(IOArgs args) { }
        protected virtual void OnDisconnect(IOArgs args) { }
        protected virtual void OnSend(IOArgs args) { }
        protected virtual void OnReceive(IOArgs args) { }
    }
}
