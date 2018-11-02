using System;
using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace ExampleArgs.Network
{
    public class IOArgs : SocketAsyncEventArgs
    {
        private IMemoryOwner<byte> _bufferOwner; //Pooled buffer owner.

        public IOHandler Handler { get; set; }

        public new byte[] Buffer
        {
            get
            {
                if (_bufferOwner == null)
                {
                    return null;
                }

                return _bufferOwner.Memory.ToArray();
            }
        }

        public Memory<byte> OperationBuffer
        {
            get
            {
                if (_bufferOwner == null)
                {
                    return Memory<byte>.Empty;
                }

                return _bufferOwner.Memory.Slice(Offset, BytesTransferred);
            }
        }

        internal IOArgs(int size = -1)
        {
            if (size > 0)
            {
                EnsureLength(size);
            }

            Completed += OnIO;
        }

        private static void OnIO(object sender, SocketAsyncEventArgs sArgs)
        {
            IOArgs args = (IOArgs)sArgs;

            if (args.Handler == null)
            {
                args.Dispose();
            }
            else
            {
                args.Handler.SubmitIO(args);
            }
        }

        public static IOArgs Alloc(int size = -1)
        {
            return IOArgsPool.Alloc(size);
        }

        public static IOArgs Alloc(IOHandler handler, int size = -1)
        {
            IOArgs args = Alloc(size);
            args.Handler = handler;

            return args;
        }

        public void EnsureLength(int count)
        {
            if (_bufferOwner == null)
            {
                _bufferOwner = SlabMemoryPool.Shared.Rent(count);
            }
            else if (_bufferOwner.Memory.Length < count)
            {
                IMemoryOwner<byte> oldOwner = _bufferOwner;
                IMemoryOwner<byte> newOwner = SlabMemoryPool.Shared.Rent(count);

                oldOwner.Memory.CopyTo(newOwner.Memory);

                _bufferOwner = newOwner;
                oldOwner.Dispose();
            }
        }

        public new void SetBuffer(Memory<byte> buffer)
        {
            EnsureLength(buffer.Length);

            buffer.CopyTo(_bufferOwner.Memory);
            base.SetBuffer(_bufferOwner.Memory.Slice(0, buffer.Length));
        }

        public new void SetBuffer(byte[] buffer, int offset, int count)
        {
            SetBuffer(new Memory<byte>(buffer, offset, count));
        }

        public void SetBuffer(string str, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            int count = encoding.GetByteCount(str);
            EnsureLength(count);
            encoding.GetBytes(str, _bufferOwner.Memory.Span);
            base.SetBuffer(_bufferOwner.Memory.Slice(0, count));
        }

        public new void SetBuffer(int offset, int count)
        {
            if (_bufferOwner == null)
            {
                throw new NullReferenceException();
            }

            base.SetBuffer(_bufferOwner.Memory.Slice(offset, count));
        }

        public void Write(Memory<byte> buffer)
        {
            int newLength = buffer.Length + Count + Offset;

            EnsureLength(newLength);
            buffer.CopyTo(_bufferOwner.Memory.Slice(Count));
            base.SetBuffer(_bufferOwner.Memory.Slice(Offset, newLength - Offset));
        }

        public void Write(string str, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            int count = encoding.GetByteCount(str);
            int newLength = count + Count + Offset;

            EnsureLength(newLength);
            encoding.GetBytes(str, _bufferOwner.Memory.Slice(Count).Span);
            base.SetBuffer(_bufferOwner.Memory.Slice(Offset, newLength - Offset));
        }

        public void Write(byte b)
        {
            int newLength = 1 + Count + Offset;

            EnsureLength(newLength);
            base.SetBuffer(_bufferOwner.Memory.Slice(Offset, newLength - Offset));
        }

        internal void BaseDispose()
        {
            base.Dispose();
        }

        public new void Dispose()
        {
            if (_bufferOwner != null)
            {
                _bufferOwner.Dispose();
                _bufferOwner = null;
            }

            AcceptSocket = null;
            RemoteEndPoint = null;
            DisconnectReuseSocket = false;
            Handler = null;

            IOArgsPool.Return(this);
        }
    }
}
