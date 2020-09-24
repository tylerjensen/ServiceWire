using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ServiceWire
{
	public class BaseCache<TKey, TValue>
	{
		ConcurrentDictionary<TKey, TValue> cache = new ConcurrentDictionary<TKey, TValue>();

		public void Add(TKey key, TValue entry)
		{
			// no additional synchronization is required on the caller side
			// does nothing if Add method is executed in parallel for the same key 
			cache.TryAdd(key, entry);
		}

		public TValue this[TKey i]
		{
			get 
			{ 
				return cache.TryGetValue(i, out TValue res)
					? res
					: default; 
			}
			set { cache[i] = value; }
		}
	}

	/// <summary>
	/// keeps Type metadata cached instead of being calculated each time
	/// </summary>
	public class TypeCacheImpl : BaseCache<Type, TypeCacheEntry>
	{ }

	/// <summary>
	/// type metadata
	/// </summary>
	public class TypeCacheEntry
	{
		public string ConfigName { get; set; }
	}
}
