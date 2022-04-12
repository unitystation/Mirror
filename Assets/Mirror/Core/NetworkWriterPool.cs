// API consistent with Microsoft's ObjectPool<T>.
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mirror
{
    /// <summary>Pool of NetworkWriters to avoid allocations.</summary>
    public static class NetworkWriterPool
    {
        // reuse Pool<T>
        // we still wrap it in NetworkWriterPool.Get/Recycle so we can reset the
        // position before reusing.
        // this is also more consistent with NetworkReaderPool where we need to
        // assign the internal buffer before reusing.
        static readonly Pool<NetworkWriterPooled> Pool = new Pool<NetworkWriterPooled>(
            () => new NetworkWriterPooled(),
            // initial capacity to avoid allocations in the first few frames
            // 1000 * 1200 bytes = around 1 MB.
            1000
        );

        /// <summary>Get a writer from the pool. Creates new one if pool is empty.</summary>
        public static NetworkWriterPooled Get()
        {
            //CUSTOM UNITYSTATION CODE// So it can be safely gotten, Without Thread funnies
            lock (Pool)
            {
                // grab from pool & reset position
                PooledNetworkWriter writer = Pool.Take();
                writer.Reset();
                writer.ClaimedThread = Thread.CurrentThread;
                return writer;
            }
        }

        /// <summary>Return a writer to the pool.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(NetworkWriterPooled writer)
        {
            //CUSTOM UNITYSTATION CODE// So it can be safely Added back, Without Thread funnies
            lock (Pool)
            {
                writer.ClaimedThread = null;
                Pool.Return(writer);
            }
        }
    }
}
