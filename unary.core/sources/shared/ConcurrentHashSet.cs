using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Unary.Core
{
    public class ConcurrentHashSet<T> : ISet<T>, IReadOnlySet<T>
        where T : notnull
    {
        private readonly ConcurrentDictionary<T, byte> _dict;

        public IEqualityComparer<T> Comparer { get; }

        public ConcurrentHashSet()
            : this((IEqualityComparer<T>)null) { }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            Comparer = comparer ?? EqualityComparer<T>.Default;
            _dict = new(Comparer);
        }

        public ConcurrentHashSet(IEnumerable<T> collection)
            : this(collection, null) { }

        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            ArgumentNullException.ThrowIfNull(collection);
            Comparer = comparer ?? EqualityComparer<T>.Default;
            _dict = new(Comparer);
            foreach (var item in collection)
                _dict.TryAdd(item, 0);
        }

        public ConcurrentHashSet(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, null) { }

        public ConcurrentHashSet(int concurrencyLevel, int capacity, IEqualityComparer<T> comparer)
        {
            Comparer = comparer ?? EqualityComparer<T>.Default;
            _dict = new(concurrencyLevel, capacity, Comparer);
        }

        public T[] ToArray()
        {
            return _dict.Keys.ToArray();
        }

        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        public bool Add(T item) => _dict.TryAdd(item, 0);
        void ICollection<T>.Add(T item) => Add(item);

        public bool Remove(T item) => _dict.TryRemove(item, out _);

        public bool Contains(T item) => _dict.ContainsKey(item);

        public void Clear() => _dict.Clear();

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            foreach (var key in _dict.Keys)
            {
                if (arrayIndex >= array.Length)
                    throw new ArgumentException("Destination array is not long enough to copy all items.");
                array[arrayIndex++] = key;
            }
        }

        public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void UnionWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                Add(item);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            foreach (var item in _dict.Keys)
                if (!otherSet.Contains(item))
                    Remove(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                Remove(item);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            foreach (var item in otherSet)
                if (!Remove(item))
                    Add(item);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            foreach (var item in _dict.Keys)
                if (!otherSet.Contains(item))
                    return false;
            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                if (!Contains(item))
                    return false;
            return true;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            return Count < otherSet.Count && IsSubsetOf(otherSet);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            return Count > otherSet.Count && IsSupersetOf(otherSet);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                if (Contains(item))
                    return true;
            return false;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = ToHashSet(other);
            if (Count != otherSet.Count)
                return false;
            foreach (var item in _dict.Keys)
                if (!otherSet.Contains(item))
                    return false;
            return true;
        }

        private HashSet<T> ToHashSet(IEnumerable<T> source)
            => new(source, Comparer);
    }
}
