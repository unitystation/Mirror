using System;
using System.Collections.Generic;
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
        private static volatile bool ZeroLocked = false;
        private static volatile bool OneLocked = false;
        private static volatile bool TwoLocked = false;
        private static volatile int LowestScore = 999999999;


        // reuse Pool<T>
        // we still wrap it in NetworkWriterPool.Get/Recycle so we can reset the
        // position before reusing.
        // this is also more consistent with NetworkReaderPool where we need to
        // assign the internal buffer before reusing.
        //CUSTOM UNITYSTATION CODE// So it can be safely gotten, Without Thread funnies and size was reduced because we don't care about some GC on the initial frames
        private static readonly List<Pool<PooledNetworkWriter>> Pools =
            new List<Pool<PooledNetworkWriter>>()
            {
                new Pool<PooledNetworkWriter>(
                    () => new PooledNetworkWriter(),
                    // initial capacity to avoid allocations in the first few frames
                    // 1000 * 1200 bytes = around 1 MB.
                    100
                ),
                new Pool<PooledNetworkWriter>(
                    () => new PooledNetworkWriter(),
                    // initial capacity to avoid allocations in the first few frames
                    // 1000 * 1200 bytes = around 1 MB.
                    100
                ),
                new Pool<PooledNetworkWriter>(
                    () => new PooledNetworkWriter(),
                    // initial capacity to avoid allocations in the first few frames
                    // 1000 * 1200 bytes = around 1 MB.
                    100
                ),

            };

        private static List<Pool<PooledNetworkWriter>> Lowest = new List<Pool<PooledNetworkWriter>>()
        {
            Pools[0],
            Pools[1],
            Pools[2]
        };

        private static volatile int LowestIndex = 0;

        /// <summary>Get a writer from the pool. Creates new one if pool is empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledNetworkWriter GetWriter()
        {
            // grab from pool & reset position
            PooledNetworkWriter writer = null;
            if (ZeroLocked == false)
            {
                lock (Pools[0])
                {
                    ZeroLocked = true;
                    writer = Pools[0].Take();
                    int Count = Pools[0].Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        LowestIndex = 0;
                    }

                    writer.Reset();
                    ZeroLocked = false;
                    return writer;
                }

            }

            if (OneLocked == false)
            {
                lock (Pools[1])
                {
                    OneLocked = true;
                    writer = Pools[1].Take();
                    int Count = Pools[1].Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        LowestIndex = 1;
                    }

                    writer.Reset();
                    OneLocked = false;
                    return writer;
                }

            }

            if (TwoLocked == false)
            {
                lock (Pools[2])
                {
                    TwoLocked = true;
                    writer = Pools[2].Take();
                    int Count = Pools[2].Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        LowestIndex = 2;
                    }

                    writer.Reset();
                    TwoLocked = false;
                    return writer;
                }

            }

            lock (Pools[0])
            {
                ZeroLocked = true;
                writer = Pools[0].Take();
                writer.Reset();
                ZeroLocked = false;
                return writer;
            }
        }



        /// <summary>Return a writer to the pool.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Recycle(PooledNetworkWriter writer)
        {
            if (writer == null) return;
            LowestScore = LowestScore + 1;
            Pool<PooledNetworkWriter> inLowest = Lowest[LowestIndex];
            lock (inLowest)
            {
                inLowest.Return(writer);
            }
        }
    }
}
