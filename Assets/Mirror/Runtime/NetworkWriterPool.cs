using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Mirror
{
    /// <summary>Pooled NetworkWriter, automatically returned to pool when using 'using'</summary>
    public sealed class PooledNetworkWriter : NetworkWriter, IDisposable
    {
        public void Dispose() => NetworkWriterPool.Recycle(this);
    }

    /// <summary>Pool of NetworkWriters to avoid allocations.</summary>
    public static class NetworkWriterPool
    {
        // reuse Pool<T>
        // we still wrap it in NetworkWriterPool.Get/Recycle so we can reset the
        // position before reusing.
        // this is also more consistent with NetworkReaderPool where we need to
        // assign the internal buffer before reusing.
        //CUSTOM UNITYSTATION CODE// So it can be safely gotten, Without Thread funnies and size was reduced because we don't care about some GC on the initial frames
        private static readonly ThreadLocal<Pool<PooledNetworkWriter>> Pool =
            new ThreadLocal<Pool<PooledNetworkWriter>>(() => new Pool<PooledNetworkWriter>(
                () => new PooledNetworkWriter(),
                // initial capacity to avoid allocations in the first few frames
                // 1000 * 1200 bytes = around 1 MB.
                1
            ));


        /// <summary>Get a writer from the pool. Creates new one if pool is empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledNetworkWriter GetWriter()
        {
            // grab from pool & reset position
            PooledNetworkWriter writer = Pool.Value.Take();
            writer.Reset();
            return writer;
        }

        /// <summary>Return a writer to the pool.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Recycle(PooledNetworkWriter writer)
        {
            Pool.Value.Return(writer);
        }
    }
}
