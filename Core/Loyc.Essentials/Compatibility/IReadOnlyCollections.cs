﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
	#if DotNet4 && !DotNet45

	/// <summary>Read-only interface defined in .NET 4.5 (just IEnumerable and Count).</summary>
	#if DotNet2 || DotNet3
	public interface IReadOnlyCollection<T> : IEnumerable<T> // 
	#else
	public interface IReadOnlyCollection<out T> : IEnumerable<T>
	#endif
	{
		int Count { get; }
	}

	/// <summary>Read-only interface defined in .NET 4.5. See also Loyc's 
	/// <see cref="Loyc.Collections.IListSource{T}"/>.</summary>
	#if DotNet2 || DotNet3
	public interface IReadOnlyList<T> : IReadOnlyCollection<T>, IEnumerable<T>
	#else
	public interface IReadOnlyList<out T> : IReadOnlyCollection<T>, IEnumerable<T>
	#endif
	{
		/// <summary>Gets the item at the specified index.</summary>
		/// <exception cref="ArgumentOutOfRangeException">The index was not valid
		/// in this list.</exception>
		/// <param name="index">An index in the range 0 to Count-1.</param>
		/// <returns>The element at the specified index.</returns>
		T this[int index] { get; }
	}

	/// <summary>Read-only dictionary interface defined in .NET 4.5</summary>
	public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
	{
		TValue this[TKey key] { get; }
		IEnumerable<TKey> Keys { get; }
		IEnumerable<TValue> Values { get; }
		bool ContainsKey(TKey key);
		bool TryGetValue(TKey key, out TValue value);
	}

	#endif
}
