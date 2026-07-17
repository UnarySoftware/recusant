using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Unary.Core
{
    public sealed class ConcurrentBiDictionary<TKey, TValue>
        : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forward;
        private readonly Dictionary<TValue, TKey> _reverse;

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public ConcurrentBiDictionary()
        {
            _forward = [];
            _reverse = [];
        }

        public ConcurrentBiDictionary(int capacity)
        {
            _forward = new Dictionary<TKey, TValue>(capacity);
            _reverse = new Dictionary<TValue, TKey>(capacity);
        }

        public ConcurrentBiDictionary(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
        {
            _forward = new Dictionary<TKey, TValue>(keyComparer);
            _reverse = new Dictionary<TValue, TKey>(valueComparer);
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _forward.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                _lock.EnterReadLock();
                try { return _forward[key]; }
                finally { _lock.ExitReadLock(); }
            }
            set
            {
                AddOrUpdate(key, value);
            }
        }

        public TKey this[TValue value]
        {
            get
            {
                _lock.EnterReadLock();
                try { return _reverse[value]; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public void Add(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_forward.ContainsKey(key))
                {
                    throw new ArgumentException($"Key '{key}' already exists.", nameof(key));
                }
                if (_reverse.ContainsKey(value))
                {
                    throw new ArgumentException($"Value '{value}' already exists.", nameof(value));
                }

                _forward[key] = value;
                _reverse[value] = key;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_forward.TryGetValue(key, out var oldValue))
                {
                    _reverse.Remove(oldValue);
                }

                if (_reverse.TryGetValue(value, out var oldKey))
                {
                    _forward.Remove(oldKey);
                }

                _forward[key] = value;
                _reverse[value] = key;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_forward.ContainsKey(key) || _reverse.ContainsKey(value))
                {
                    return false;
                }

                _forward[key] = value;
                _reverse[value] = key;
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool RemoveByKey(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_forward.TryGetValue(key, out var value))
                {
                    return false;
                }
                _forward.Remove(key);
                _reverse.Remove(value);
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool RemoveByValue(TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_reverse.TryGetValue(value, out var key))
                {
                    return false;
                }
                _reverse.Remove(value);
                _forward.Remove(key);
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _forward.Clear(); _reverse.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }

        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try { return _forward.ContainsKey(key); }
            finally { _lock.ExitReadLock(); }
        }

        public bool ContainsValue(TValue value)
        {
            _lock.EnterReadLock();
            try { return _reverse.ContainsKey(value); }
            finally { _lock.ExitReadLock(); }
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            _lock.EnterReadLock();
            try { return _forward.TryGetValue(key, out value); }
            finally { _lock.ExitReadLock(); }
        }

        public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key)
        {
            _lock.EnterReadLock();
            try { return _reverse.TryGetValue(value, out key); }
            finally { _lock.ExitReadLock(); }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _lock.EnterReadLock();
            List<KeyValuePair<TKey, TValue>> snapshot;
            try { snapshot = [.. _forward]; }
            finally { _lock.ExitReadLock(); }
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public override string ToString()
        {
            return $"ConcurrentBiDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>[{Count}]";
        }
    }
}
