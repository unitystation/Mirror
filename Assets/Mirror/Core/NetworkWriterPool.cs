// API consistent with Microsoft's ObjectPool<T>.
using System.Runtime.CompilerServices;

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

        //CUSTOM UNITYSTATION CODE// So it can be safely gotten, Without Thread funnies
        public class ObjectPool<T>
        {
            private readonly ConcurrentBag<T> _objects;
            private readonly Func<T> _objectGenerator;

            public ObjectPool(Func<T> objectGenerator, int Size )
            {
                _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
                _objects = new ConcurrentBag<T>();
            }

            public T Take() => _objects.TryTake(out T item) ? item : _objectGenerator();

            public void Return(T item) => _objects.Add(item);
        }


        /// <summary>Get a writer from the pool. Creates new one if pool is empty.</summary>
        public static NetworkWriterPooled Get()
        {
            // grab from pool & reset position
            PooledNetworkWriter writer = Pool.Take();
            writer.Reset();
            return writer;
        }

        /// <summary>Return a writer to the pool.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(NetworkWriterPooled writer)
        {
            Pool.Return(writer);
        }
    }
}
