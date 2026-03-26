using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unary.Core
{
    public struct UpdaterHandle
    {
        public float Delay;
        public float Range;
        public int Pool;
    }

    public interface IProcess
    {
        void Process(float delta);
    }

    public interface IPhysicsProcess
    {
        void PhysicsProcess(float delta);
    }

    public interface IUpdateInvoker<T> where T : class
    {
        static abstract void Invoke(T target, float delta);
    }

    public struct ProcessInvoker : IUpdateInvoker<IProcess>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(IProcess target, float delta) => target.Process(delta);
    }

    public struct PhysicsProcessInvoker : IUpdateInvoker<IPhysicsProcess>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(IPhysicsProcess target, float delta) => target.PhysicsProcess(delta);
    }

    public sealed class DelayedUnit(float delay)
    {
        private readonly List<Action<float>>[] _updaters = CreatePools();
        private int _subscribeIndex = 0;
        private readonly float _delay = delay;

        private float _timer = 0.0f;
        private bool _processing = false;
        private int _processingCount = 0;

        private static List<Action<float>>[] CreatePools()
        {
            var pools = new List<Action<float>>[Updater.PoolCount];

            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = [];
            }

            return pools;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float delta)
        {
            _timer += delta;

            if (!_processing)
            {
                if (_timer >= _delay)
                {
                    _processing = true;
                }
            }
            else
            {
                var span = CollectionsMarshal.AsSpan(_updaters[_processingCount]);

                for (int i = 0; i < span.Length; i++)
                {
                    span[i](_timer);
                }

                _processingCount++;

                if (_processingCount == Updater.PoolCount)
                {
                    _processingCount = 0;
                    _processing = false;
                    _timer = 0.0f;
                }
            }
        }

        public int Subscribe(Action<float> action)
        {
            _updaters[_subscribeIndex].Add(action);
            int result = _subscribeIndex;

            if (++_subscribeIndex == Updater.PoolCount)
            {
                _subscribeIndex = 0;
            }

            return result;
        }

        public void Unsubscribe(Action<float> action, int pool)
        {
            var list = _updaters[pool];
            int idx = list.IndexOf(action);
            if (idx >= 0)
            {
                // Swap with the last element and remove the last element to avoid shifting the list
                list[idx] = list[^1];
                list.RemoveAt(list.Count - 1);
            }
        }
    }

    public sealed class RangeUnit(float delay, float range)
    {
        private enum State
        {
            PreStarting,
            Starting,
            PreEnding,
            Ending
        };

        private readonly List<Action<float>>[] _starters = CreatePools();
        private readonly List<Action<float>>[] _enders = CreatePools();
        private int _subscribeIndex = 0;
        private readonly float _delay = delay;

        private float _timer = 0.0f;
        private int _processingCount = 0;

        private State _state = State.PreStarting;
        private readonly float _range = range;

        private static List<Action<float>>[] CreatePools()
        {
            var pools = new List<Action<float>>[Updater.PoolCount];

            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = [];
            }

            return pools;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float delta)
        {
            _timer += delta;

            switch (_state)
            {
                default:
                case State.PreStarting:
                    {
                        if (_timer >= _delay)
                        {
                            _state = State.Starting;
                        }
                        break;
                    }
                case State.Starting:
                    {
                        var span = CollectionsMarshal.AsSpan(_starters[_processingCount]);

                        for (int i = 0; i < span.Length; i++)
                        {
                            span[i](_timer);
                        }

                        _processingCount++;

                        if (_processingCount == Updater.PoolCount)
                        {
                            _processingCount = 0;
                            _state = State.PreEnding;
                            _timer = 0.0f;
                        }
                        break;
                    }
                case State.PreEnding:
                    {
                        if (_timer >= _range)
                        {
                            _state = State.Ending;
                        }
                        break;
                    }
                case State.Ending:
                    {
                        var span = CollectionsMarshal.AsSpan(_enders[_processingCount]);

                        for (int i = 0; i < span.Length; i++)
                        {
                            span[i](_timer);
                        }

                        _processingCount++;

                        if (_processingCount == Updater.PoolCount)
                        {
                            _processingCount = 0;
                            _state = State.PreStarting;
                            _timer = 0.0f;
                        }
                        break;
                    }
            }
        }

        public int Subscribe(Action<float> starting, Action<float> ending)
        {
            _starters[_subscribeIndex].Add(starting);
            _enders[_subscribeIndex].Add(ending);
            int result = _subscribeIndex;

            if (++_subscribeIndex == Updater.PoolCount)
            {
                _subscribeIndex = 0;
            }

            return result;
        }

        public void Unsubscribe(Action<float> starting, Action<float> ending, int pool)
        {
            var starterList = _starters[pool];
            int idx = starterList.IndexOf(starting);
            if (idx >= 0)
            {
                // Swap with the last element and remove the last element to avoid shifting the list
                starterList[idx] = starterList[^1];
                starterList.RemoveAt(starterList.Count - 1);
            }

            var enderList = _enders[pool];
            idx = enderList.IndexOf(ending);
            if (idx >= 0)
            {
                // Swap with the last element and remove the last element to avoid shifting the list
                enderList[idx] = enderList[^1];
                enderList.RemoveAt(enderList.Count - 1);
            }
        }
    }

    public struct UpdaterUnitKey : IEquatable<UpdaterUnitKey>
    {
        public float Delay;
        public float Range;

        public bool Equals(UpdaterUnitKey other)
        {
            return Delay == other.Delay && Range == other.Range;
        }

        public override bool Equals(object obj)
        {
            return obj is UpdaterUnitKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Delay, Range);
        }

        public override string ToString()
        {
            return $"(Delay: {Delay}, Range: {Range})";
        }

        public static bool operator ==(UpdaterUnitKey left, UpdaterUnitKey right) => left.Equals(right);
        public static bool operator !=(UpdaterUnitKey left, UpdaterUnitKey right) => !left.Equals(right);
    }

    public sealed class UpdaterGroup<TSubscriber, TInvoker>
        where TSubscriber : class
        where TInvoker : IUpdateInvoker<TSubscriber>
    {
        private readonly List<TSubscriber> _subscribers = [];

        private readonly Dictionary<UpdaterUnitKey, DelayedUnit> _delayedDictionary = [];
        private readonly List<DelayedUnit> _delayedList = [];

        private readonly Dictionary<UpdaterUnitKey, RangeUnit> _rangesDictionary = [];
        private readonly List<RangeUnit> _rangesList = [];

        private UpdaterUnitKey _key = new();

        public void Update(float delta)
        {
            var subscriberSpan = CollectionsMarshal.AsSpan(_subscribers);

            for (int i = 0; i < subscriberSpan.Length; i++)
            {
                TInvoker.Invoke(subscriberSpan[i], delta);
            }

            Span<DelayedUnit> delayedSpan = CollectionsMarshal.AsSpan(_delayedList);

            for (int i = 0; i < delayedSpan.Length; i++)
            {
                delayedSpan[i].Update(delta);
            }

            Span<RangeUnit> rangesSpan = CollectionsMarshal.AsSpan(_rangesList);

            for (int i = 0; i < rangesSpan.Length; i++)
            {
                rangesSpan[i].Update(delta);
            }
        }

        public void Subscribe(TSubscriber subscriber) => _subscribers.Add(subscriber);

        public void Unsubscribe(TSubscriber subscriber)
        {
            int idx = _subscribers.IndexOf(subscriber);

            if (idx >= 0)
            {
                _subscribers[idx] = _subscribers[^1];
                _subscribers.RemoveAt(_subscribers.Count - 1);
            }
        }

        public UpdaterHandle SubscribeDelayed(float delay, Action<float> action)
        {
            _key.Delay = delay;
            _key.Range = 0.0f;

            if (!_delayedDictionary.TryGetValue(_key, out var unit))
            {
                unit = new DelayedUnit(delay);
                _delayedDictionary[_key] = unit;
                _delayedList.Add(unit);
            }

            return new()
            {
                Delay = delay,
                Range = 0.0f,
                Pool = unit.Subscribe(action)
            };
        }

        public UpdaterHandle SubscribeRange(float delay, float range, Action<float> starter, Action<float> ending)
        {
            _key.Delay = delay;
            _key.Range = range;

            if (!_rangesDictionary.TryGetValue(_key, out var unit))
            {
                unit = new RangeUnit(delay, range);
                _rangesDictionary[_key] = unit;
                _rangesList.Add(unit);
            }

            return new()
            {
                Delay = delay,
                Range = range,
                Pool = unit.Subscribe(starter, ending)
            };
        }

        public void UnsubscribeDelayed(UpdaterHandle handle, Action<float> action)
        {
            _key.Delay = handle.Delay;
            _key.Range = handle.Range;

            if (_delayedDictionary.TryGetValue(_key, out var unit))
            {
                unit.Unsubscribe(action, handle.Pool);
            }
        }

        public void UnsubscribeRange(UpdaterHandle handle, Action<float> starter, Action<float> ending)
        {
            _key.Delay = handle.Delay;
            _key.Range = handle.Range;

            if (_rangesDictionary.TryGetValue(_key, out var unit))
            {
                unit.Unsubscribe(starter, ending, handle.Pool);
            }
        }
    }

    [Tool]
    [GlobalClass]
    public partial class Updater : Node, ICoreSystem
    {
        public const int PoolCount = 5;

        public UpdaterGroup<IProcess, ProcessInvoker> Process { get; private set; } = new();
        public UpdaterGroup<IPhysicsProcess, PhysicsProcessInvoker> PhysicsProcess { get; private set; } = new();

        void ISystem.Process(float delta)
        {
            Process.Update(delta);
        }

        void ISystem.PhysicsProcess(float delta)
        {
            PhysicsProcess.Update(delta);
        }
    }
}
