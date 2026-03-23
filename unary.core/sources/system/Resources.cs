using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    [GlobalClass]
    public partial class Resources : Node, ICoreSystem
    {
        private readonly Dictionary<long, HashSet<ResourcePatch>> _patches = [];

        private ResourceInterceptor _interceptor;

        bool ISystem.Initialize()
        {
            List<ResourcePatch> patches = ResourceTypesManager.Singleton.LoadResourcesOfType<ResourcePatch>(false);

            foreach (var patch in patches)
            {
                if (string.IsNullOrEmpty(patch.Target.TargetValue))
                {
                    continue;
                }

                long id = ResourceUid.Singleton.TextToId(patch.Target.TargetValue);

                if (id == ResourceUid.InvalidId)
                {
                    this.Warning($"Ignoring invalid patch target {patch.Target.TargetValue}");
                    continue;
                }

                if (!_patches.TryGetValue(id, out var entries))
                {
                    entries = [];
                    _patches[id] = entries;
                }

                entries.Add(patch);
            }

            _interceptor = new();
            ResourceInterceptor.Enabled = true;
            ResourceLoader.Singleton.AddResourceFormatLoader(_interceptor, true);

            return true;
        }

        void ISystem.Deinitialize()
        {
            ResourceLoader.Singleton.RemoveResourceFormatLoader(_interceptor);
        }

        public Resource LoadPatched(string path, string typeHint = "")
        {
            Resource result = ResourceLoader.Singleton.Load(path, typeHint, ResourceLoader.CacheMode.IgnoreDeep);
            _interceptor.TemporaryClear();
            return result;
        }

        private struct AsyncEntry
        {
            public Action<Resource> Result;
            public Action<float> Progress;
            public Godot.Collections.Array FloatStorage;
        }

        private readonly ConcurrentDictionary<string, AsyncEntry> _asyncEntries = [];
        private readonly ConcurrentBag<string> _deleteEntries = [];

        public void LoadPatchedAsync(string path, Action<Resource> result, Action<float> progress, string typeHint = "")
        {
            RuntimeLogger.OnLog.StartQueue();

            Error error = ResourceLoader.Singleton.LoadThreadedRequest(path, typeHint, true, ResourceLoader.CacheMode.IgnoreDeep);

            if (error != Error.Ok)
            {
                progress(0.0f);
                result(null);
                return;
            }

            _asyncEntries[path] = new()
            {
                Result = result,
                Progress = progress,
                FloatStorage = []
            };
        }

        private bool _hadEntries = false;

        void ISystem.Process(float delta)
        {
            foreach (var entry in _asyncEntries)
            {
                var status = ResourceLoader.Singleton.LoadThreadedGetStatus(entry.Key, entry.Value.FloatStorage);

                if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    entry.Value.Progress(entry.Value.FloatStorage[0].As<float>());
                }
                else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    Resource result = ResourceLoader.Singleton.LoadThreadedGet(entry.Key);

                    entry.Value.Result(result);
                    entry.Value.Progress(1.0f);

                    _deleteEntries.Add(entry.Key);
                }
                else
                {
                    entry.Value.Result(null);
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
                RuntimeLogger.OnLog.ProcessQueue();
            }
        }

        public void Process(Resource resource)
        {
            long id = ResourceLoader.Singleton.GetResourceUid(resource.ResourcePath);

            if (id == ResourceUid.InvalidId)
            {
                return;
            }

            if (!_patches.TryGetValue(id, out var patches))
            {
                return;
            }

            foreach (var patch in patches)
            {
                foreach (var property in patch.Properties)
                {
                    try
                    {
                        resource.Set(property.Key, property.Value);
                    }
                    catch (Exception e)
                    {
                        this.Error(e.Message);
                    }
                }
            }
        }
    }
}
