using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    public abstract class BaseEvent<T, K>
        where T : Delegate
    {
        protected readonly List<Type> _subscriberTypes = [];
        protected readonly List<T> _subscriberFuncs = [];

        protected object _queueLock = new();
        protected readonly List<K> _initQueue = [];
        protected bool _initialized = false;
        private bool _subscribed = false;
        protected bool _collecting = false;
        protected object _owner;
        protected bool _debug = false;
        protected StringBuilder _debugString;
        protected Type _defineType;
        protected string _defineField;
        protected string _source;

        public void StartQueue()
        {
            _collecting = true;
        }

        public abstract void ProcessQueue();

        protected bool PublishInternal(K data)
        {
            if (_initialized && !_collecting)
            {
                return true;
            }

            if (Bootstrap.Singleton.FinishedInitialization && !_collecting)
            {
                _initialized = true;
                return true;
            }

            if (!_subscribed && !_collecting)
            {
                Bootstrap.Singleton.OnFinishInitialization += OnFinishInitialization;
                _subscribed = true;
            }

            lock (_queueLock)
            {
                bool found = false;

                foreach (var entry in _initQueue)
                {
                    if (entry.Equals(data))
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    _initQueue.Add(data);
                }
            }

            return false;
        }

        private void OnFinishInitialization()
        {
            if (_initialized)
            {
                Bootstrap.Singleton.OnFinishInitialization -= OnFinishInitialization;
                return;
            }

            ProcessQueue();

            _initQueue.Clear();
            _initialized = true;
        }

        private int FindIndexByType(Type obj, bool forwardOrder)
        {
            if (forwardOrder)
            {
                for (int i = 0; i < _subscriberTypes.Count; i++)
                {
                    if (_subscriberTypes[i] == obj)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = _subscriberTypes.Count - 1; i >= 0; i--)
                {
                    if (_subscriberTypes[i] == obj)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void SubscribeIndexed(int insertIndex, T func, Type subscriberType)
        {
            if (insertIndex < 0)
            {
                insertIndex = 0;
            }

            if (insertIndex >= _subscriberTypes.Count)
            {
                _subscriberTypes.Add(subscriberType);
                _subscriberFuncs.Add(func);
            }
            else
            {
                _subscriberTypes.Insert(insertIndex, subscriberType);
                _subscriberFuncs.Insert(insertIndex, func);
            }
        }

        public void SubscribeBefore(Type before, T func, object subscriber)
        {
            int index = FindIndexByType(before, true);

            if (index != -1)
            {
                SubscribeIndexed(index - 1, func, subscriber.GetType());
            }
        }

        public void SubscribeAfter(Type after, T func, object subscriber)
        {
            int index = FindIndexByType(after, false);

            if (index != -1)
            {
                SubscribeIndexed(index + 1, func, subscriber.GetType());
            }
        }

        public void Subscribe(T func, object subscriber)
        {
            _subscriberTypes.Add(subscriber.GetType());
            _subscriberFuncs.Add(func);
        }

        public void Unsubscribe(object subscriber)
        {
            Type type = subscriber.GetType();

            int index = FindIndexByType(type, false);

            while (index != -1)
            {
                _subscriberTypes.RemoveAt(index);
                _subscriberFuncs.RemoveAt(index);
                index = FindIndexByType(type, false);
            }
        }
    }

    public delegate bool EmptyEventDelegate();

    public class EventAction : BaseEvent<EmptyEventDelegate, object>
    {
        public EventAction() : base()
        {

        }

        public EventAction(Type defineType, string defineField) : base()
        {
            _debug = true;
            _defineType = defineType;
            _defineField = defineField;

            _debugString = new();

            _debugString.Append(_defineType.FullName).Append('.').Append(_defineField);

            _source = _debugString.ToString();
            _debugString.Clear();
        }

        public void Publish()
        {
            if (!PublishInternal(null))
            {
                return;
            }

            if (_debug)
            {
                _debugString.Append("Dispatching order:\n\n");
            }

            int counter = 1;

            foreach (EmptyEventDelegate handler in _subscriberFuncs)
            {
                if (_debug)
                {
                    _debugString.Append(counter).Append(". ").Append(handler.Target.GetType().FullName).Append('\n');
                }
                if (!handler())
                {
                    break;
                }

                counter++;
            }

            if (_debug)
            {
                RuntimeLogger.Log(_source, _debugString.ToString());
                _debugString.Clear();
            }
        }

        public override void ProcessQueue()
        {
            if (_debug)
            {
                _debugString.Append("Dispatching order (queue):\n");
            }

            for (int i = 0; i < _initQueue.Count; i++)
            {
                if (_debug)
                {
                    _debugString.Append('\n');
                }

                int counter = 1;

                foreach (EmptyEventDelegate handler in _subscriberFuncs)
                {
                    if (_debug)
                    {
                        _debugString.Append(counter).Append(". ").Append(handler.Target.GetType().FullName).Append('\n');
                    }
                    if (!handler())
                    {
                        break;
                    }

                    counter++;
                }
            }

            if (_debug)
            {
                RuntimeLogger.Log(_source, _debugString.ToString());
                _debugString.Clear();
            }

            _collecting = false;
        }
    }

    public delegate bool DataEventDelegate<T>(ref T data) where T : struct;

    public class EventFunc<T> : BaseEvent<DataEventDelegate<T>, T>
        where T : struct
    {
        public EventFunc() : base()
        {

        }

        public EventFunc(Type defineType, string defineField) : base()
        {
            if (typeof(T) == typeof(RuntimeLogger.LogEventData))
            {
                // Drop LogEventData attempt at debugging or we will get stack overflows
                return;
            }

            _debug = true;
            _defineType = defineType;
            _defineField = defineField;

            _debugString = new();

            _debugString.Append(_defineType.FullName).Append('.').Append(_defineField);

            _source = _debugString.ToString();
            _debugString.Clear();
        }

        public void Publish(T data)
        {
            if (!PublishInternal(data))
            {
                return;
            }

            if (_debug)
            {
                _debugString.Append("Dispatching order:\n\n");
            }

            int counter = 1;

            foreach (DataEventDelegate<T> handler in _subscriberFuncs)
            {
                if (_debug)
                {
                    _debugString.Append(counter).Append(". ").Append(handler.Target.GetType().FullName).Append('\n');
                }

                if (!handler(ref data))
                {
                    break;
                }

                counter++;
            }

            if (_debug)
            {
                RuntimeLogger.Log(_source, _debugString.ToString());
                _debugString.Clear();
            }
        }

        public override void ProcessQueue()
        {
            if (_debug)
            {
                _debugString.Append("Dispatching order (queue):\n");
            }

            foreach (var entry in _initQueue)
            {
                T target = entry;

                if (_debug)
                {
                    _debugString.Append('\n');
                }

                int counter = 1;

                foreach (DataEventDelegate<T> handler in _subscriberFuncs)
                {
                    if (_debug)
                    {
                        _debugString.Append(counter).Append(". ").Append(handler.Target.GetType().FullName).Append('\n');
                    }
                    if (!handler(ref target))
                    {
                        break;
                    }

                    counter++;
                }
            }

            if (_debug)
            {
                RuntimeLogger.Log(_source, _debugString.ToString());
                _debugString.Clear();
            }

            _collecting = false;
        }
    }
}
