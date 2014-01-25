using System;
using System.Collections.Concurrent;

namespace ServiceWire
{
    public class PooledDictionary<TKey, TValue> 
    {
        private readonly ConcurrentDictionary<TKey, ConcurrentQueue<TValue>> _dq;
        private readonly int _concurrencyLevel;
        private readonly int _size;

        public PooledDictionary()
        {
            _concurrencyLevel = Environment.ProcessorCount * 8;
            _size = _concurrencyLevel * _concurrencyLevel;
            _dq = new ConcurrentDictionary<TKey, ConcurrentQueue<TValue>>(_concurrencyLevel, _size);
        }

        public void Add(TKey key, TValue value)
        {
            if (!_dq.ContainsKey(key)) _dq.TryAdd(key, new ConcurrentQueue<TValue>());
            ConcurrentQueue<TValue> q;
            if (_dq.TryGetValue(key, out q))
            {
                q.Enqueue(value);
            }
            else
            {
                throw new ArgumentException("Unable to add value");
            }
        }

        public int Count(TKey key)
        {
            if (!_dq.ContainsKey(key)) _dq.TryAdd(key, new ConcurrentQueue<TValue>());
            ConcurrentQueue<TValue> q;
            if (_dq.TryGetValue(key, out q))
            {
                return q.Count;
            }
            return 0;
        }

        public TValue Request(TKey key, Func<TValue> creator = null)
        {
            if (!_dq.ContainsKey(key)) _dq.TryAdd(key, new ConcurrentQueue<TValue>());
            ConcurrentQueue<TValue> q;
            if (_dq.TryGetValue(key, out q))
            {
                TValue v;
                if (q.TryDequeue(out v)) return v;
                if (null != creator) return creator();
            }
            return default(TValue);
        }

        public void Release(TKey key, TValue value)
        {
            Add(key, value); //just adds it back to key's queue
        }
    }
}
