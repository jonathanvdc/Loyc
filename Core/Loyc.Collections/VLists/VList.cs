/*
	VList processing library: Copyright 2009 by David Piepgrass

	This library is free software: you can redistribute it and/or modify it 
	it under the terms of the GNU Lesser General Public License as published 
	by the Free Software Foundation, either version 3 of the License, or (at 
	your option) any later version. It is provided without ANY warranties.
	Please note that it is fairly complex. Therefore, it may contain bugs 
	despite my best efforts to test it.

	If you did not receive a copy of the License with this library, you can 
	find it at http://www.gnu.org/licenses/
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Loyc.MiniTest;
using Loyc.Collections;

namespace Loyc.Collections
{
	/// <summary>
	/// VList represents a reference to a reverse-order FVList.
	/// </summary><remarks>
	/// An <a href="http://www.codeproject.com/Articles/26171/VList-data-structures-in-C">article</a>
	/// is available online about the VList data types.
	/// <para/>
	/// The VList is a persistent list data structure described in Phil Bagwell's 
	/// 2002 paper "Fast Functional Lists, Hash-Lists, Deques and Variable Length
	/// Arrays". Originally, this type was called RVList because it works in the
	/// reverse order to the original VList type: new items are normally added at
	/// the <i>beginning</i> of a VList, which is normal in functional languages,
	/// but <i>this</i> VList acts like a normal .NET list, so it is optimized for
	/// new items to be added at the end. The name "RVList" is ugly, though, since
	/// it misleadingly appears to be related to Recreational Vehicles. So as 
	/// of LeMP 1.5, it's called simply VList.
	/// <para/>
	/// In contrast, the <see cref="FVList{T}"/> type acts like the original VList;
	/// its Add method puts new items at the beginning (index 0).
	/// <para/>
	/// See the remarks of <see cref="VListBlock{T}"/> for a more detailed 
	/// description.
	/// </remarks>
	[DebuggerTypeProxy(typeof(CollectionDebugView<>)),
	 DebuggerDisplay("Count = {Count}")]
	public struct VList<T> : IListAndListSource<T>, ICloneable<VList<T>>, ICloneable
	{
		internal VListBlock<T> _block;
		internal int _localCount;

		#region Constructors

		internal VList(VListBlock<T> block, int localCount)
		{
			_block = block;
			_localCount = localCount;
		}
		public VList(T firstItem)
		{
			_block = new VListBlockOfTwo<T>(firstItem, false);
			_localCount = 1;
		}
		public VList(T itemZero, T itemOne)
		{
			_block = new VListBlockOfTwo<T>(itemZero, itemOne, false);
			_localCount = 2;
		}
		public VList(T[] array)
		{
			_block = null;
			_localCount = 0;
			for (int i = 0; i < array.Length; i++)
				Add(array[i]);
		}
		public VList(IEnumerable<T> list)
		{
			_block = null;
			_localCount = 0;
			AddRange(list);
		}
		public VList(VList<T> list)
		{
			this = list;
		}

		#endregion

		#region Obtaining sublists

		public VList<T> WithoutLast(int offset)
		{
			return VListBlock<T>.SubList(_block, _localCount, offset).ToVList();
		}
		/// <summary>Returns a list without the last item. If the list is empty, 
		/// an empty list is retured.</summary>
		public VList<T> Tail
		{
			get {
				return VListBlock<T>.TailOf(ToFVList()).ToVList();
			}
		}
		public VList<T> NextIn(VList<T> largerList)
		{
			return VListBlock<T>.BackUpOnce(this, largerList);
		}
		public VList<T> First(int count)
		{
			int c = Count;
			if (count >= c)
				return this;
			if (count <= 0)
				return Empty;
			return WithoutLast(c - count);
		}

		#endregion

		#region Equality testing and GetHashCode()

		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator ==(VList<T> lhs, VList<T> rhs)
		{
			return lhs._localCount == rhs._localCount && lhs._block == rhs._block;
		}
		/// <summary>Returns whether the two list references are different.
		/// Does not compare the contents of the lists.</summary>
		public static bool operator !=(VList<T> lhs, VList<T> rhs)
		{
			return lhs._localCount != rhs._localCount || lhs._block != rhs._block;
		}
		/// <summary>Returns whether the two list references are the same.
		/// Does not compare the contents of the lists.</summary>
		public override bool Equals(object rhs_)
		{
			try {
				VList<T> rhs = (VList<T>)rhs_;
				return this == rhs;
			} catch(InvalidCastException) {
				return false;
			}
		}
		public override int GetHashCode()
		{
			Debug.Assert((_localCount == 0) == (_block == null));
			if (_block == null)
				return 2357; // any ol' number will do
			return _block.GetHashCode() ^ _localCount;
		}

		#endregion

		#region AddRange, InsertRange, RemoveRange

		public VList<T> AddRange(VList<T> list) { return AddRange(list, new VList<T>()); }
		public VList<T> AddRange(VList<T> list, VList<T> excludeSubList)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list.ToFVList(), excludeSubList.ToFVList()).ToVList();
			return this;
		}
		public VList<T> AddRange(IList<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list, true).ToVList();
			return this;
		}
		public VList<T> AddRange(IEnumerable<T> list)
		{
			this = VListBlock<T>.AddRange(_block, _localCount, list.GetEnumerator());
			return this;
		}
		public VList<T> InsertRange(int index, IList<T> list)
		{
			this = VListBlock<T>.InsertRange(_block, _localCount, list, Count - index, true).ToVList();
			return this;
		}
		public VList<T> RemoveRange(int index, int count)
		{
			if (count != 0)
				this = _block.RemoveRange(_localCount, Count - (index + count), count).ToVList();
			return this;
		}

		#endregion

		#region Other stuff

		/// <summary>Returns the last item of the list (at index Count-1), which is the head of the list.</summary>
		public T Last
		{
			get {
				return _block.Front(_localCount);
			}
		}
		public bool IsEmpty
		{
			get {
				Debug.Assert((_localCount == 0) == (_block == null));
				return _block == null;
			}
		}
		/// <summary>Removes the last item (at index Count-1) from the list and returns it.</summary>
		public T Pop()
		{
			if (_block == null)
				throw new InvalidOperationException("Pop: The list is empty.");
			T item = Last;
			this = WithoutLast(1);
			return item;
		}
		/// <summary>Synonym for Add(); adds an item to the front of the list.</summary>
		public VList<T> Push(T item) { return Add(item); }

		/// <summary>Returns this list as a FVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>This is a trivial operation; the FVList shares the same memory.</remarks>
		public static explicit operator FVList<T>(VList<T> list)
		{
			return new FVList<T>(list._block, list._localCount);
		}
		/// <summary>Returns this list as a FVList, which effectively reverses the
		/// order of the elements.</summary>
		/// <returns>This is a trivial operation; the FVList shares the same memory.</returns>
		public FVList<T> ToFVList()
		{
			return new FVList<T>(_block, _localCount);
		}

		/// <summary>Returns this list as a FWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public static explicit operator FWList<T>(VList<T> list) { return list.ToFWList(); }
		/// <summary>Returns this list as a FWList, which effectively reverses the
		/// order of the elements.</summary>
		/// <remarks>The list contents are not copied until you modify the FWList.</remarks>
		public FWList<T> ToFWList()
		{
			return new FWList<T>(_block, _localCount, false);
		}

		/// <summary>Returns this list as an WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public static explicit operator WList<T>(VList<T> list) { return list.ToWList(); }
		/// <summary>Returns this list as an WList.</summary>
		/// <remarks>The list contents are not copied until you modify the WList.</remarks>
		public WList<T> ToWList()
		{
			return new WList<T>(_block, _localCount, false);
		}

		/// <summary>Returns the VList converted to an array.</summary>
		public T[] ToArray()
		{
			return VListBlock<T>.ToArray(_block, _localCount, true);
		}

		/// <summary>Gets the number of blocks used by this list.</summary>
		/// <remarks>You might look at this property when optimizing your program,
		/// because the runtime of some operations increases as the chain length 
		/// increases. This property runs in O(BlockChainLength) time. Ideally,
		/// BlockChainLength is proportional to log_2(Count), but certain VList 
		/// usage patterns can produce long chains.</remarks>
		public int BlockChainLength
		{
			get { return _block == null ? 0 : _block.ChainLength; }
		}

		public static readonly VList<T> Empty = new VList<T>();

		#endregion

		#region IList<T> Members

        /// <summary>Searches for the specified object and returns the zero-based
        /// index of the first occurrence (lowest index) within the entire
        /// VList.</summary>
        /// <param name="item">Item to locate (can be null if T can be null)</param>
        /// <returns>Index of the item, or -1 if it was not found.</returns>
        /// <remarks>This method determines equality using the default equality
        /// comparer EqualityComparer.Default for T, the type of values in the list.
        ///
        /// This method performs a linear search, and is typically an O(n)
        /// operation, where n is Count. However, because the list is searched
        /// upward from index 0 to Count-1, if the list's blocks do not increase in
        /// size exponentially (due to the way that the list has been modified in
        /// the past), the search can have worse performance; the (unlikely) worst
        /// case is O(n^2). FVList(of T).IndexOf() doesn't have this problem.
        /// </remarks>
		public int IndexOf(T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int i = 0;
			foreach (T candidate in this) {
				if (comparer.Equals(candidate, item))
					return i;
				i++;
			}
			return -1;
		}

		void IList<T>.Insert(int index, T item) { Insert(index, item); }
		public VList<T> Insert(int index, T item)
		{
			_block = VListBlock<T>.Insert(_block, _localCount, item, Count - index);
			_localCount = _block.ImmCount;
			return this;
		}

		void IList<T>.RemoveAt(int index) { RemoveAt(index); }
		public VList<T> RemoveAt(int index)
		{
			this = _block.RemoveAt(_localCount, Count - (index + 1)).ToVList();
			return this;
		}

		public T this[int index]
		{
			get {
				return _block.RGet(index, _localCount);
			}
			set {
				this = _block.ReplaceAt(_localCount, value, Count - 1 - index).ToVList();
			}
		}
		/// <summary>Gets an item from the list at the specified index; returns 
		/// defaultValue if the index is not valid.</summary>
		public T this[int index, T defaultValue]
		{
			get {
				if (_block != null)
					_block.RGet(index, _localCount, ref defaultValue);
				return defaultValue;
			}
		}

		#endregion

		#region ICollection<T> Members

		/// <summary>Inserts an item at the back (index Count) of the VList.</summary>
		void ICollection<T>.Add(T item) { Add(item); }
		/// <summary>Inserts an item at the back (index Count) of the VList.</summary>
		public VList<T> Add(T item)
		{
			_block = VListBlock<T>.Add(_block, _localCount, item);
			_localCount = _block.ImmCount;
			return this;
		}

		void ICollection<T>.Clear() { Clear(); }
		public VList<T> Clear()
		{
			_block = null;
			_localCount = 0;
			return this;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T item in this)
				array[arrayIndex++] = item;
		}

		public int Count
		{
			get {
				Debug.Assert((_localCount == 0) == (_block == null));
				if (_block == null)
					return 0;
				return _localCount + _block.PriorCount; 
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}

		#endregion

		#region IEnumerable<T> Members

        /// <summary>Enumerates through a VList from index 0 up to index Count-1.
        /// </summary><remarks>
        /// Normally, enumerating the list takes O(Count + log(Count)^2) = O(Count)
		/// time. However, if the list's block chain does not increase in size 
		/// exponentially (due to the way that the list has been modified in the 
		/// past), the search can have worse performance; the worst case is O(n^2),
		/// but this is unlikely. FVList's Enumerator doesn't have this problem 
		/// because it enumerates in the other direction.</remarks>
		public struct Enumerator : IEnumerator<T>
		{
			ushort _localIndex;
			ushort _localCount;
			VListBlock<T> _curBlock;
			VListBlock<T> _nextBlock;
			FVList<T> _outerList;

			public Enumerator(VList<T> list)
				: this(new FVList<T>(list._block, list._localCount), new FVList<T>()) { }
			public Enumerator(VList<T> list, VList<T> subList)
				: this(new FVList<T>(list._block, list._localCount),
					   new FVList<T>(subList._block, subList._localCount)) { }
			public Enumerator(FVList<T> list) : this(list, new FVList<T>()) { }
			public Enumerator(FVList<T> list, FVList<T> subList)
			{
				_outerList = list;
				int localCount;
				_nextBlock = VListBlock<T>.FindNextBlock(ref subList, _outerList, out localCount)._block;
				_localIndex = (ushort)(checked((ushort)subList._localCount) - 1);
				_curBlock = subList._block;
				_localCount = checked((ushort)localCount);
			}

			public T Current
			{
				get { return _curBlock[_localIndex]; }
			}
			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}
			public bool MoveNext()
			{
				if (++_localIndex >= _localCount) {
					_curBlock = _nextBlock;
					if (_curBlock == null)
						return false;

					int localCount;
					// The FVList constructed here usually violates the invariant
					// (_localCount == 0) == (_block == null), but FindNextBlock
					// doesn't mind. It's necessary to avoid the "subList is not
					// within list" exception in all cases.
					FVList<T> subList = new FVList<T>(_curBlock, 0);
					_nextBlock = VListBlock<T>.FindNextBlock(
						ref subList, _outerList, out localCount)._block;
					_localCount = checked((ushort)localCount);
					_localIndex = 0;
				}
				return true;
			}
			public void Reset()
			{
				throw new NotImplementedException();
			}
			public void Dispose()
			{
			}
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IListSource<T> Members

		public T TryGet(int index, out bool fail)
		{
			T value = default(T);
			fail = _block == null || !_block.RGet(index, _localCount, ref value);
			return value;
		}

		IRange<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int count = int.MaxValue) { return new Slice_<T>(this, start, count); }
		
		#endregion 

		#region ICloneable Members

		public VList<T> Clone() { return this; }
		object ICloneable.Clone() { return this; }

		#endregion

		#region Optimized LINQ-like methods

        /// <summary>Applies a filter to a list, to exclude zero or more
        /// items.</summary>
        /// <param name="keep">A function that chooses which items to include
        /// (exclude items by returning false).</param>
        /// <returns>The list after filtering has been applied. The original list
        /// structure is not modified.</returns>
		/// <remarks>
		/// If the predicate keeps the first N items it is passed, those N items are
		/// typically not copied, but shared between the existing list and the new 
		/// one.
		/// </remarks>
		public VList<T> Where(Predicate<T> keep)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.Where(_localCount, keep, null);
		}

		/// <summary>Filters and maps a list with a user-defined function.</summary>
		/// <param name="filter">A function that chooses which items to include
		/// in a new list, and what to change them to.</param>
		/// <returns>The list after filtering has been applied. The original list
		/// structure is not modified.</returns>
		/// <remarks>
		/// This is a smart function. If the filter does not modify the first N 
		/// items it is passed, those N items are typically not copied, but shared 
		/// between the existing list and the new one.
		/// </remarks>
		public VList<T> WhereSelect(Func<T,Maybe<T>> filter)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.WhereSelect(_localCount, filter, null);
		}

		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		/// <remarks>
		/// This method is called "Smart" because of what happens if the map
		/// doesn't do anything. If the map function returns the first N items
		/// unmodified, those N items are typically not copied, but shared between
		/// the existing list and the new one. This is useful for functional code
		/// that sometimes processes a list without modifying it at all.
		/// </remarks>
		public VList<T> SmartSelect(Func<T, T> map)
		{
			if (_localCount == 0)
				return this;
			else
				return (VList<T>)_block.SmartSelect(_localCount, map, null);
		}

		/// <summary>Maps a list to another list of the same length.</summary>
		/// <param name="map">A function that transforms each item in the list.</param>
		/// <returns>The list after the map function is applied to each item. The 
		/// original VList structure is not modified.</returns>
		public VList<Out> Select<Out>(Func<T, Out> map)
		{
			return (VList<Out>)VListBlock<T>.Select<Out>(_block, _localCount, map, null);
		}

		/// <summary>Transforms a list (combines filtering with selection and more).</summary>
		/// <param name="x">Method to apply to each item in the list</param>
		/// <returns>A list formed from transforming all items in the list</returns>
		/// <remarks>See the documentation of FVList.Transform() for more information.</remarks>
		public VList<T> Transform(VListTransformer<T> x)
		{
			return (VList<T>)VListBlock<T>.Transform(_block, _localCount, x, true, null);
		}

		#endregion
	}

	[TestFixture]
	public class RVListTests
	{
		[Test]
		public void SimpleTests()
		{
			// In this simple test, I only add and remove items from the back
			// of a VList, but forking is also tested.

			VList<int> list = new VList<int>();
			Assert.That(list.IsEmpty);

			// Adding to VListBlockOfTwo
			list = new VList<int>(10, 20);
			ExpectList(list, 10, 20);

			list = new VList<int>();
			list.Add(1);
			Assert.That(!list.IsEmpty);
			list.Add(2);
			ExpectList(list, 1, 2);

			// A fork in VListBlockOfTwo. Note that list2 will use two VListBlocks
			// here but list will only use one.
			VList<int> list2 = list.WithoutLast(1);
			list2.Add(3);
			ExpectList(list, 1, 2);
			ExpectList(list2, 1, 3);

			// Try doubling list2
			list2.AddRange(list2);
			ExpectList(list2, 1, 3, 1, 3);

			// list now uses two arrays
			list.Add(4);
			ExpectList(list, 1, 2, 4);

			// Try doubling list using a different overload of AddRange()
			list.AddRange((IList<int>)list);
			ExpectList(list, 1, 2, 4, 1, 2, 4);
			list = list.WithoutLast(3);
			ExpectList(list, 1, 2, 4);

			// Remove(), Pop()
			Assert.AreEqual(3, list2.Pop());
			ExpectList(list2, 1, 3, 1);
			Assert.That(!list2.Remove(0));
			Assert.AreEqual(1, list2.Pop());
			Assert.That(list2.Remove(3));
			ExpectList(list2, 1);
			Assert.That(list2.Remove(1));
			ExpectList(list2);
			AssertThrows<Exception>(delegate() { list2.Pop(); });

			// Add many, SubList(). This will fill 3 arrays (sizes 8, 4, 2) and use
			// 1 element of a size-16 array. Oh, and test the enumerator.
			for (int i = 5; i <= 16; i++)
				list.Add(i);
			ExpectList(list, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
			list2 = list.WithoutLast(6);
			ExpectListByEnumerator(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10);
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[-1]; });
			AssertThrows<IndexOutOfRangeException>(delegate() { int i = list[15]; });

			// IndexOf, contains
			Assert.That(list.Contains(11));
			Assert.That(!list2.Contains(11));
			Assert.That(list[list.IndexOf(2)] == 2);
			Assert.That(list[list.IndexOf(1)] == 1);
			Assert.That(list[list.IndexOf(15)] == 15);
			Assert.That(list.IndexOf(3) == -1);

			// PreviousIn(), Back
			VList<int> list3 = list2;
			Assert.AreEqual(11, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(12, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(13, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(14, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(15, (list3 = list3.NextIn(list)).Last);
			Assert.AreEqual(16, (list3 = list3.NextIn(list)).Last);
			AssertThrows<Exception>(delegate() { list3.NextIn(list); });

			// Next
			Assert.AreEqual(10, (list3 = list3.WithoutLast(6)).Last);
			Assert.AreEqual(9, (list3 = list3.Tail).Last);
			Assert.AreEqual(8, (list3 = list3.Tail).Last);
			Assert.AreEqual(7, (list3 = list3.Tail).Last);
			Assert.AreEqual(6, (list3 = list3.Tail).Last);
			Assert.AreEqual(5, (list3 = list3.Tail).Last);
			Assert.AreEqual(4, (list3 = list3.Tail).Last);
			Assert.AreEqual(2, (list3 = list3.Tail).Last);
			Assert.AreEqual(1, (list3 = list3.Tail).Last);
			Assert.That((list3 = list3.Tail).IsEmpty);

			// list2 is still the same
			ExpectList(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10);

			// ==, !=, Equals(), AddRange(a, b)
			Assert.That(!list2.Equals("hello"));
			list3 = list2;
			Assert.That(list3.Equals(list2));
			Assert.That(list3 == list2);
			// This AddRange forks the list. List2 ends up with block sizes 8 (3
			// used), 8 (3 used), 4, 2.
			list2.AddRange(list2, list2.WithoutLast(3));
			ExpectList(list2, 1, 2, 4, 5, 6, 7, 8, 9, 10, 8, 9, 10);
			Assert.That(list3 != list2);

			// List3 is a sublist of list, but list2 no longer is
			Assert.That(list3.NextIn(list).Last == 11);
			AssertThrows<InvalidOperationException>(delegate() { list2.NextIn(list); });

			list2 = list2.WithoutLast(3);
			Assert.That(list3 == list2);
		}

		private void AssertThrows<Type>(Action @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}

		private static void ExpectList<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], list[i]);
		}
		private static void ExpectListByEnumerator<T>(IList<T> list, params T[] expected)
		{
			Assert.AreEqual(expected.Length, list.Count);
			int i = 0;
			foreach (T item in list) {
				Assert.AreEqual(expected[i], item);
				i++;
			}
		}

		[Test]
		public void TestInsertRemove()
		{
			VList<int> list = new VList<int>(9);
			VList<int> list2 = new VList<int>(10, 11);
			list.Insert(0, 12);
			list.Insert(1, list2[1]);
			list.Insert(2, list2[0]);
			ExpectList(list, 12, 11, 10, 9);
			for (int i = 0; i < 9; i++)
				list.Insert(4, i);
			ExpectList(list, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

			list2 = list;
			for (int i = 1; i <= 6; i++)
				list2.RemoveAt(i);
			ExpectList(list2, 12, 10, 8, 6, 4, 2, 0);
			ExpectList(list, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0); // unchanged

			Assert.AreEqual(0, list2.Pop());
			list2.Insert(5, -2);
			ExpectList(list2, 12, 10, 8, 6, 4, -2, 2);
			list2.Insert(5, -1);
			ExpectList(list2, 12, 10, 8, 6, 4, -1, -2, 2);

			// Test changing items
			list = list2;
			for (int i = 0; i < list.Count; i++)
				list[i] = i;
			ExpectList(list, 0, 1, 2, 3, 4, 5, 6, 7);
			ExpectList(list2, 12, 10, 8, 6, 4, -1, -2, 2);

			list2.Clear();
			ExpectList(list2);
			Assert.AreEqual(5, list[5]);
		}

		[Test]
		public void TestInsertRemoveRange()
		{
			VList<int> oneTwo = new VList<int>(1, 2);
			VList<int> threeFour = new VList<int>(3, 4);
			VList<int> list = oneTwo;
			VList<int> list2 = threeFour;

			ExpectList(list, 1, 2);
			list.InsertRange(1, threeFour);
			ExpectList(list, 1, 3, 4, 2);
			list2.InsertRange(2, oneTwo);
			ExpectList(list2, 3, 4, 1, 2);

			list.RemoveRange(1, 2);
			ExpectList(list, 1, 2);
			list2.RemoveRange(2, 2);
			ExpectList(list2, 3, 4);

			list.RemoveRange(0, 2);
			ExpectList(list);
			list2.RemoveRange(1, 1);
			ExpectList(list2, 3);

			list = oneTwo;
			list.AddRange(threeFour);
			ExpectList(list, 1, 2, 3, 4);
			list.InsertRange(1, list);
			ExpectList(list, 1, 1, 2, 3, 4, 2, 3, 4);
			list.RemoveRange(1, 1);
			list.RemoveRange(4, 3);
			ExpectList(list, 1, 2, 3, 4);

			list.RemoveRange(0, 4);
			ExpectList(list);

			list2.InsertRange(0, list);
			list2.InsertRange(1, list);
			ExpectList(list2, 3);
		}

		[Test]
		public void TestEmptyListOperations()
		{
			VList<int> a = new VList<int>();
			VList<int> b = new VList<int>();
			a.AddRange(b);
			a.InsertRange(0, b);
			a.RemoveRange(0, 0);
			Assert.That(!a.Remove(0));
			Assert.That(a.IsEmpty);

			a.Add(1);
			b.AddRange(a);
			ExpectList(b, 1);
			b.RemoveAt(0);
			Assert.That(b.IsEmpty);
			b.InsertRange(0, a);
			ExpectList(b, 1);
			b.RemoveRange(0, 1);
			Assert.That(b.IsEmpty);
			b.Insert(0, a[0]);
			ExpectList(b, 1);
			b.Remove(a.Last);
			Assert.That(b.IsEmpty);
			
			AssertThrows<InvalidOperationException>(delegate() { a.NextIn(b); });
		}

		[Test]
		public void TestToArray()
		{
			VList<int> list = new VList<int>();
			int[] array = list.ToArray();
			Assert.AreEqual(array.Length, 0);

			array = list.Add(1).ToArray();
			ExpectList(array, 1);

			array = list.Add(2).ToArray();
			ExpectList(array, 1, 2);

			array = list.Add(3).ToArray();
			ExpectList(array, 1, 2, 3);

			array = list.AddRange(new int[] { 4, 5, 6, 7, 8 }).ToArray();
			ExpectList(array, 1, 2, 3, 4, 5, 6, 7, 8);
		}

		[Test]
		void TestAddRangePair()
		{
			VList<int> list = new VList<int>();
			VList<int> list2 = new VList<int>();
			list2.AddRange(new int[] { 1, 2, 3, 4 });
			list.AddRange(list2, list2.WithoutLast(1));
			list.AddRange(list2, list2.WithoutLast(2));
			list.AddRange(list2, list2.WithoutLast(3));
			list.AddRange(list2, list2.WithoutLast(4));
			ExpectList(list, 1, 2, 3, 1, 2, 1);

			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(list2.WithoutLast(1), list2); });
			AssertThrows<InvalidOperationException>(delegate() { list2.AddRange(VList<int>.Empty, list2); });
		}
		
		[Test]
		public void TestSublistProblem()
		{
			// This problem affects FVList.PreviousIn(), VList.NextIn(),
			// AddRange(list, excludeSubList), VList.Enumerator when used with a
			// range.

			// Normally this works fine:
			VList<int> subList = new VList<int>(), list;
			subList.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7 });
			list = subList;
			list.Add(8);
			Assert.That(subList.NextIn(list).Last == 8);

			// But try it a second time and the problem arises, without some special
			// code in VListBlock<T>.FindNextBlock() that has been added to
			// compensate. I call the problem copy-causing-sharing-failure. You see,
			// right now subList is formed from three blocks: a size-8 block that
			// contains {7}, a size-4 block {3, 4, 5, 6} and a size-2 block {1, 2}.
			// But the size-8 block actually has two items {7, 8} and when we
			// attempt to add 9, a new array must be created. It might waste a lot
			// of memory to make a new block {9} that links to the size-8 block that
			// contains {7}, so instead a new size-8 block {7, 9} is created that
			// links directly to {3, 4, 5, 6}. That way, the block {7, 8} can be
			// garbage-collected if it is no longer in use. But a side effect is
			// that subList no longer appears to be a part of list. The fix is to
			// notice that list (block {7, 9}) and subList (block that contains {7})
			// have the same prior list, {3, 4, 5, 6}, and that the remaining 
			// item(s) in subList (just one item, {7}, in this case) are also
			// present in list.
			list = subList;
			list.Add(9);
			Assert.AreEqual(9, subList.NextIn(list).Last);
		}

		[Test]
		public void TestExampleTransforms()
		{
			// These examples are listed in the documentation of FVList.Transform().
			// There are more Transform() tests in VListTests() and RWListTests().

			VList<int> list = new VList<int>(new int[] { -1, 2, -2, 13, 5, 8, 9 });
			VList<int> output;

			output = list.Transform((int i, ref int n) =>
			{   // Keep every second item
			    return (i % 2) == 1 ? XfAction.Keep : XfAction.Drop;
			});
			ExpectList(output, 2, 13, 8);
			
			output = list.Transform((int i, ref int n) =>
			{   // Keep odd numbers
			    return (n % 2) != 0 ? XfAction.Keep : XfAction.Drop;
			});
			ExpectList(output, -1, 13, 5, 9);
			
			output = list.Transform((int i, ref int n) =>
			{   // Keep and square all odd numbers
			    if ((n % 2) != 0) {
			        n *= n;
			        return XfAction.Change;
			    } else
			        return XfAction.Drop;
			});
			ExpectList(output, 1, 169, 25, 81);
			
			output = list.Transform((int i, ref int n) =>
			{   // Increase each item by its index
			    n += i;
			    return i == 0 ? XfAction.Keep : XfAction.Change;
			});
			ExpectList(output, -1, 3, 0, 16, 9, 13, 15);

			list = new VList<int>(new int[] { 1, 2, 3 });

			output = list.Transform(delegate(int i, ref int n) {
				return i >= 0 ? XfAction.Repeat : XfAction.Keep;
			});
			ExpectList(output, 1, 1, 2, 2, 3, 3);

			output = list.Transform(delegate(int i, ref int n) {
				if (i >= 0) 
				 return XfAction.Repeat;
				n *= 10;
				return XfAction.Change;
			});
			ExpectList(output, 1, 10, 2, 20, 3, 30);

			output = list.Transform(delegate (int i, ref int n) {
				if (i >= 0) {
				 n *= 10;
				 return XfAction.Repeat;
				}
				return XfAction.Keep;
			});
			ExpectList(output, 10, 1, 20, 2, 30, 3);

			output = list.Transform(delegate (int i, ref int n) {
				n *= 10;
				if (n > 1000)
				 return XfAction.Drop;
				return XfAction.Repeat;
			});
			ExpectList(output, 10, 100, 1000, 20, 200, 30, 300);
		}
	}
}
