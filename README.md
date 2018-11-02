# SocketArgsLowAllocationLib
High Performance and low allocation wrapper for SocketAsyncEventArgs.

This library uses new C# features such as Memory<byte>, MemoryPool<byte>, Span<byte> to prevent unnecessary allocations.
The memory pool is same as Kestrel's which is a SlabMemoryPool, which is used to prevent GC fragmentation.

Example can be found in TestArgs folder.

Memory usage with Echo server with 20k clients.
One buffer is used per client, and the buffer size is 4096 bytes.
![GitHub Logo](/images/diagnostics.png)
