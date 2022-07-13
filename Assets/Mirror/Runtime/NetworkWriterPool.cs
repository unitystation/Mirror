using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

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
        private static readonly Pool<PooledNetworkWriter> PoolZero =
        new Pool<PooledNetworkWriter>(
        () => new PooledNetworkWriter(),
            // initial capacity to avoid allocations in the first few frames
            // 1000 * 1200 bytes = around 1 MB.
            100
            );

        private static readonly Pool<PooledNetworkWriter> PoolOne =
            new Pool<PooledNetworkWriter>(
                () => new PooledNetworkWriter(),
                // initial capacity to avoid allocations in the first few frames
                // 1000 * 1200 bytes = around 1 MB.
                100
            );


        private static readonly Pool<PooledNetworkWriter> PoolTwo =
            new Pool<PooledNetworkWriter>(
                () => new PooledNetworkWriter(),
                // initial capacity to avoid allocations in the first few frames
                // 1000 * 1200 bytes = around 1 MB.
                100
            );

        private static Pool<PooledNetworkWriter> Lowest = PoolOne;

        /// <summary>Get a writer from the pool. Creates new one if pool is empty.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledNetworkWriter GetWriter()
        {
            // grab from pool & reset position
            PooledNetworkWriter writer = null;
            if (ZeroLocked == false)
            {
                lock (PoolZero)
                {
                    ZeroLocked = true;
                    writer = PoolZero.Take();
                    int Count = PoolZero.Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        lock (Lowest)
                        {
                            Lowest = PoolZero;
                        }
                    }

                    if (writer == null)
                    {
                        Debug.Log("AAAAAAAAA");
                    }
                    writer.Reset();
                    ZeroLocked = false;
                    return writer;
                }

            }

            if (OneLocked == false)
            {
                lock (PoolOne)
                {
                    OneLocked = true;
                    writer = PoolOne.Take();
                    int Count = PoolOne.Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        lock (Lowest)
                        {
                            Lowest = PoolOne;
                        }
                    }

                    writer.Reset();
                    OneLocked = false;
                    return writer;
                }

            }

            if (TwoLocked == false)
            {
                lock (PoolTwo)
                {
                    TwoLocked = true;
                    writer = PoolTwo.Take();
                    int Count = PoolTwo.Count;
                    if (Count < LowestScore)
                    {
                        LowestScore = Count;
                        lock (Lowest)
                        {
                            Lowest = PoolTwo;
                        }

                    }

                    writer.Reset();
                    TwoLocked = false;
                    return writer;
                }

            }

            lock (PoolOne)
            {
                ZeroLocked = true;
                writer = PoolOne.Take();
                writer.Reset();
                ZeroLocked = false;
                return writer;
            }
        }



        /// <summary>Return a writer to the pool.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Recycle(PooledNetworkWriter writer)
        {
            LowestScore = LowestScore + 1;
            lock (Lowest)
            {
                Lowest.Return(writer);
            }

        }
    }
}
