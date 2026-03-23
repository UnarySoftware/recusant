using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Unary.Core
{
    [GlobalClass]
    public partial class ResourceInterceptor : ResourceFormatLoader
    {
        [ThreadStatic]
        public static bool Enabled;

        public override string[] _GetRecognizedExtensions()
        {
            return ["tres", "res"];
        }

        public override bool _HandlesType(StringName type)
        {
            return true;
        }

        public override string _GetResourceType(string path)
        {
            return "";
        }

        [ThreadStatic]
        private static bool _bypassing;

        private Resources _processor = null;

        private readonly ConcurrentDictionary<string, Resource> _temporaryCache = [];

        public void TemporaryClear()
        {
            _temporaryCache.Clear();
        }

        public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
        {
            if (!Enabled || _bypassing)
            {
                // We're in a recursive call - let it fall through to the next loader
                return default;
            }

            _processor ??= Resources.Singleton;

            _bypassing = true;

            Resource resource;

            try
            {
                ResourceLoader.CacheMode mode = (ResourceLoader.CacheMode)cacheMode;

                // Temporary load
                if (mode == ResourceLoader.CacheMode.IgnoreDeep)
                {
                    if (_temporaryCache.TryGetValue(path, out var tempResult))
                    {
                        // TODO Multithread this
                        //RuntimeLogger.Log(this, $"[TEMP CACHE FETCH]: {originalPath}");
                        resource = tempResult;
                    }
                    else
                    {
                        //RuntimeLogger.Log(this, $"[NEW TEMP LOAD]: {originalPath}");
                        resource = ResourceLoader.Singleton.Load(originalPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
                        _temporaryCache[path] = resource;
                    }
                }
                else
                {
                    // We are only interested in deep temporary loads - those indicate that we want resource patching
                    //RuntimeLogger.Log(this, $"[REUSE CACHE FETCH]: {originalPath}");
                    return default;
                }
            }
            finally
            {
                _bypassing = false;
            }

            _processor.Process(resource);

            return resource;
        }
    }
}
