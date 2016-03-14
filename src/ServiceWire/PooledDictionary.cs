#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.Collections.Concurrent;

#endregion


namespace ServiceWire
{
    public sealed class PooledDictionary<TKey,TValue>:IDisposable
    {
        #region Constractor

        public PooledDictionary()
        {
            _concurrencyLevel=Environment.ProcessorCount*8;
            _size=_concurrencyLevel*_concurrencyLevel;
            _dq=new ConcurrentDictionary<TKey,ConcurrentQueue<TValue>>(_concurrencyLevel,_size);
        }

        #endregion


        #region Fields

        private readonly ConcurrentDictionary<TKey,ConcurrentQueue<TValue>> _dq;
        private readonly int _concurrencyLevel;
        private readonly int _size;

        #endregion


        #region Methods


        #region Public Methods

        public void Add(TKey key,TValue value)
        {
            if(!_dq.ContainsKey(key))
            {
                _dq.TryAdd(key,new ConcurrentQueue<TValue>());
            }
            ConcurrentQueue<TValue> q;
            if(_dq.TryGetValue(key,out q))
            {
                q.Enqueue(value);
            } else
            {
                throw new ArgumentException("Unable to add value");
            }
        }

        public int Count(TKey key)
        {
            if(!_dq.ContainsKey(key))
            {
                _dq.TryAdd(key,new ConcurrentQueue<TValue>());
            }
            ConcurrentQueue<TValue> q;
            if(_dq.TryGetValue(key,out q))
            {
                return q.Count;
            }
            return 0;
        }

        public TValue Request(TKey key,Func<TValue> creator=null)
        {
            if(!_dq.ContainsKey(key))
            {
                _dq.TryAdd(key,new ConcurrentQueue<TValue>());
            }
            ConcurrentQueue<TValue> q;
            if(_dq.TryGetValue(key,out q))
            {
                TValue v;
                if(q.TryDequeue(out v))
                {
                    return v;
                }
                if(null!=creator)
                {
                    return creator();
                }
            }
            return default(TValue);
        }

        public void Release(TKey key,TValue value)
        {
            Add(key,value); //just adds it back to key's queue
        }

        #endregion


        #endregion


        #region IDisposable Members

        protected bool _disposed;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            if(!_disposed)
            {
                _disposed=true;
                foreach(var kvp in _dq)
                {
                    while(!kvp.Value.IsEmpty)
                    {
                        TValue v;
                        if(kvp.Value.TryDequeue(out v))
                        {
                            var disp=v as IDisposable;
                            if(null!=disp)
                            {
                                disp.Dispose();
                            }
                        }
                    }
                }
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}