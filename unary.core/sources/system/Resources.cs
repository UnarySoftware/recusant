using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Unary.Core
{
    [GlobalClass]
    public partial class Resources : Node, ICoreSystem
    {
        private ResourcePatcher _patcher;

#if TOOLS
        [InitializeExplicit(typeof(ResourceManager))]
#endif
        bool ISystem.Initialize()
        {
            _patcher = new();
            ResourceLoader.Singleton.AddDetour(_patcher, true);
            return true;
        }

        void ISystem.Deinitialize()
        {
            ResourceLoader.Singleton.RemoveDetour(_patcher);
        }

        private struct AsyncEntry
        {
            public Action<Resource, object> Result;
            public Action<float> Progress;
            public Godot.Collections.Array FloatStorage;
            public object Data;
        }

        private readonly ConcurrentDictionary<string, AsyncEntry> _asyncEntries = [];
        private readonly ConcurrentHashSet<string> _deleteEntries = [];

        public void LoadAsync(string path, Action<Resource, object> result, Action<float> progress, string typeHint = "", object data = null)
        {
            RuntimeLogger.OnLog.StartQueue();

            Error error = ResourceLoader.Singleton.LoadThreadedRequest(path, typeHint, false);

            if (error != Error.Ok)
            {
                this.Error($"Failed to load a resource at \"{path}\"");
                progress(0.0f);
                result(null, null);

                // Nothing was queued, so Process will never flush the log queue that StartQueue opened above
                RuntimeLogger.OnLog.PublishQueue();
                return;
            }

            _asyncEntries[path] = new()
            {
                Result = result,
                Progress = progress,
                FloatStorage = [],
                Data = data
            };
        }

        private bool _hadEntries = false;

        void ISystem.Process(float delta)
        {
            foreach (var entry in _asyncEntries)
            {
                ResourceLoader.ThreadLoadStatus status = ResourceLoader.Singleton.LoadThreadedGetStatus(entry.Key, entry.Value.FloatStorage);

                if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    entry.Value.Progress(entry.Value.FloatStorage[0].As<float>());
                }
                else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    Resource result = ResourceLoader.Singleton.LoadThreadedGet(entry.Key);

                    entry.Value.Result(result, entry.Value.Data);
                    entry.Value.Progress(1.0f);

                    _deleteEntries.Add(entry.Key);
                }
                else
                {
                    entry.Value.Result(null, null);
                    entry.Value.Progress(0.0f);

                    this.Error($"Failed to get a level load status due to an error \"{status}\"");
                    _deleteEntries.Add(entry.Key);
                }
            }

            foreach (var deletion in _deleteEntries)
            {
                _asyncEntries.Remove(deletion, out var _);
            }

            _deleteEntries.Clear();

            if (!_asyncEntries.IsEmpty)
            {
                _hadEntries = true;
            }
            else if (_asyncEntries.IsEmpty && _hadEntries)
            {
                _hadEntries = false;
                RuntimeLogger.OnLog.PublishQueue();
            }
        }
    }
}
