using System;
using System.Collections.Concurrent;

namespace ServiceWire
{
    public sealed class PooledDictionary<TKey, TValue> : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, ConcurrentQueue<TValue>> _dq;
        public PooledDictionary()
        {
            _dq = new ConcurrentDictionary<TKey, ConcurrentQueue<TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            var q = _dq.GetOrAdd(key, k => new ConcurrentQueue<TValue>());
            q.Enqueue(value);
        }

        public int Count(TKey key)
        {
            return _dq.GetOrAdd(key, k => new ConcurrentQueue<TValue>()).Count;
        }

        public TValue Request(TKey key, Func<TValue> creator = null)
        {
            var q = _dq.GetOrAdd(key, k => new ConcurrentQueue<TValue>());
            TValue v;
            if (q.TryDequeue(out v)) return v;
            if (null != creator) return creator();
            return default(TValue);
        }

        public void Release(TKey key, TValue value)
        {
            Add(key, value); //just adds it back to key's queue
        }

        #region IDisposable Members

        private bool _disposed = false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            if (!_disposed)
            {
                _disposed = true;
                foreach(var kvp in _dq)
                {
                    while (!kvp.Value.IsEmpty)
                    {
                        TValue v;
                        if (kvp.Value.TryDequeue(out v))
                        {
                            var disp = v as IDisposable;
                            if (null != disp) disp.Dispose();
                        }
                    }
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
