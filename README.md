# SocketArgsLowAllocationLib
High Performance and low allocation wrapper for SocketAsyncEventArgs.

This library uses new C# features such as Memory<byte>, MemoryPool<byte>, Span<byte> to prevent unnecessary allocations.
The memory pool is same as Kestrel's which is a SlabMemoryPool, which is used to prevent GC fragmentation.

Example can be found in TestArgs folder.

Memory usage with Echo server with 20k clients.
One IOArgs is used per client. The IOArgs use buffer which is 4096 bytes.
![GitHub Logo](/images/diagnostics.png)

-------------

Working with IOArgs:
```cs
public IOArgs ArgsFromStringExample()
{
  IOArgs args = IOArgs.Alloc(); //Alloc gets args from shared pool with no buffer.
  args.SetBuffer("Hello"); //Sets the buffer with string. If there is no buffer, it will be taken from shared memory.
  args.Write("from"); //Writes string to the buffer. Necessary extends buffer size, by getting new buffer from pool.
  args.Write("world!"); //Same here. You also use diffrent Encoding, by giving Encoding parameter.
  args.Write((byte)'\n'); //End our args to new line;
  //You calso .Write Memory<byte>.
  
  return args;
}

class TcpConnection : TcpHandler
{
  private int _sendCount = 0; //Counter for our completed sends.

  public EchoConnection(IOQueue queue) : base(queue)
  {
    //Initializes new EchoConnection
    //Queue is single threaded IO hander which handles all events IOArgs submitted to it.
    
    //The TcpHandler.On*** methods will always be ran from IOQueue's thread.
    //All methods by TcpHandler are thread safe.
  }
  
  protected override void OnConnect(IOArgs args)
  {
    IOArgs exampleStringArgs = ArgsFromStringExample(); //User our example
  
    SendArgs(exampleStringArgs); //Send our example string.
    Send("And bye bye :v)!"); //You can send Memory<byte> or strings, which will be copied to pooled buffer
  
    args.Dispose(); //Dispose the args used for connection. This will return resources to pools.
  }
  
  protected override void OnSend(IOArgs args)
  {
      //Get packet from args.OperationBuffer.
      //OperationBuffer = Memory.Slice(args.Offset, args.BytesTransferred)
      string sentPacket = Encoding.ASCII.GetString(args.OperationBuffer.Span); //Get string from span
      Console.WriteLine(sentPacket); //Write on console
      
      //REMEMBER!!!!!:
      //Dispose always args, if you are not going to use it anymore.
      //This will release all resources back to their pools.
      args.Dispose();
      
      //Increase our _sendCount counter.
      IncreaseCount();
  }
  
  private void IncreaseCount() 
  {
    _sendCount += 1;
    
    if (_sendCount == 2) 
    {
      Dispose(); //This is thread safe, OnDispose will be called only once.
    }
  }
  
  protected override void OnDispose()
  {
    Console.WriteLine("Dispose completed!");
  }
}
```
