using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Unary.Core
{
    public sealed class BiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forward;
        private readonly Dictionary<TValue, TKey> _reverse;

        public int Count => _forward.Count;
        public IReadOnlyCollection<TKey> Keys => _forward.Keys;
        public IReadOnlyCollection<TValue> Values => _forward.Values;

        public BiDictionary()
        {
            _forward = [];
            _reverse = [];
        }

        public BiDictionary(int capacity)
        {
            _forward = new Dictionary<TKey, TValue>(capacity);
            _reverse = new Dictionary<TValue, TKey>(capacity);
        }

        public BiDictionary(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
        {
            _forward = new Dictionary<TKey, TValue>(keyComparer);
            _reverse = new Dictionary<TValue, TKey>(valueComparer);
        }

        public BiDictionary(IDictionary<TKey, TValue> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _forward = new Dictionary<TKey, TValue>(source.Count);
            _reverse = new Dictionary<TValue, TKey>(source.Count);

            foreach (var (key, value) in source)
            {
                Add(key, value);
            }
        }

        public TValue this[TKey key]
        {
            get => _forward[key];
            set => AddOrUpdate(key, value);
        }

        public TKey this[TValue value] => _reverse[value];

        public void Add(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            if (_forward.ContainsKey(key))
            {
                throw new ArgumentException(
                    $"An entry with key '{key}' already exists.", nameof(key));
            }

            if (_reverse.ContainsKey(value))
            {
                throw new ArgumentException(
                    $"An entry with value '{value}' already exists.", nameof(value));
            }

            _forward[key] = value;
            _reverse[value] = key;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            // Remove the stale reverse entry for the old value of this key.
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

        public bool TryAdd(TKey key, TValue value)
        {
            if (_forward.ContainsKey(key) || _reverse.ContainsKey(value))
            {
                return false;
            }

            _forward[key] = value;
            _reverse[value] = key;
            return true;
        }

        public bool RemoveByKey(TKey key)
        {
            if (!_forward.TryGetValue(key, out var value))
            {
                return false;
            }

            _forward.Remove(key);
            _reverse.Remove(value);
            return true;
        }

        public bool RemoveByValue(TValue value)
        {
            if (!_reverse.TryGetValue(value, out var key))
            {
                return false;
            }

            _reverse.Remove(value);
            _forward.Remove(key);
            return true;
        }

        public void Clear()
        {
            _forward.Clear();
            _reverse.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _forward.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return _reverse.ContainsKey(value);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return _forward.TryGetValue(key, out value);
        }

        public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key)
        {
            return _reverse.TryGetValue(value, out key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _forward.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return $"BiDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>[{Count}]";
        }
    }
}
