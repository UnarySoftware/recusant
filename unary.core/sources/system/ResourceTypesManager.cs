using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Unary.Core
{
    public partial class ResourceTypesManager : Node, ICoreSystem
    {
        public readonly struct ResourceHandle(string modId, string path, Type type) : IEquatable<ResourceHandle>
        {
            public string ModId { get; init; } = modId;
            public string Path { get; init; } = path;
            public Type Type { get; init; } = type;

            public readonly bool Equals(ResourceHandle other)
            {
                return ModId == other.ModId && Path == other.Path && Type == other.Type;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is ResourceHandle other && Equals(other);
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(ModId, Path, Type);
            }

            public static bool operator ==(ResourceHandle left, ResourceHandle right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ResourceHandle left, ResourceHandle right)
            {
                return !(left == right);
            }
        }

        private readonly Dictionary<string, ResourceTypesManifest> _modIdTomanifest = [];
        private readonly Dictionary<Type, List<ResourceHandle>> _typesToResources = [];

        public List<ResourceHandle> GetResourceHandlesOfType(Type type)
        {
            if (_typesToResources.TryGetValue(type, out var result))
            {
                return result;
            }
            return [];
        }

        public List<Type> GetResourceTypesAssignableFrom(Type type)
        {
            List<Type> result = [];

            foreach (var typeGroup in _typesToResources)
            {
                if (type.IsAssignableFrom(typeGroup.Key))
                {
                    result.Add(typeGroup.Key);
                }
            }

            return result;
        }

        public List<T> LoadResources<T>(bool patched = true) where T : BaseResource
        {
            List<T> result = [];

            List<BaseResource> targets = LoadResourcesByType(typeof(T), patched);

            foreach (var target in targets)
            {
                result.Add((T)target);
            }

            return result;
        }

        public List<BaseResource> LoadResourcesByType(Type type, bool patched = true)
        {
            List<BaseResource> result = [];

            List<ResourceHandle> handles = GetResourceHandlesOfType(type);

            foreach (var handle in handles)
            {
                // This is considered a valid non-error-worthy behaviour because the file
                // could have been deleted by some mod but still be present in a types manifest
                if (!ResourceLoader.Singleton.Exists(handle.Path))
                {
                    continue;
                }

                Resource resource = ResourceLoader.Singleton.Load(handle.Path, type.Name);

                if (resource == null)
                {
                    continue;
                }

                BaseResource baseResource = (BaseResource)resource;
                baseResource.ModId = handle.ModId;

                result.Add(baseResource);
            }

            return result;
        }

        public bool InitializeMod(string modId)
        {
            string targetPath = modId + '/' + modId + ResourceTypesManifest.Extension;

            if (!File.Exists(targetPath))
            {
                this.Critical($"Failed to find manifest file with resource types for a mod \"{modId}\"");
                return false;
            }

            if (_modIdTomanifest.ContainsKey(modId))
            {
                this.Critical($"There is a duplicate manifest file with resource types for a mod \"{modId}\"");
                return false;
            }

            ResourceTypesManifest manifest = JsonSerializer.Deserialize<ResourceTypesManifest>(File.ReadAllText(targetPath));

            manifest.Paths ??= [];
            manifest.Types ??= [];
            manifest.ModId = modId.ToLower();

            _modIdTomanifest[modId] = manifest;

            if (manifest.Paths.Length != manifest.Types.Length)
            {
                this.Critical($"Resource manifest at path \"{targetPath}\" has {manifest.Paths.Length} paths and {manifest.Types.Length} types.");
                return false;
            }

            for (int i = 0; i < manifest.Paths.Length; i++)
            {
                string path = manifest.Paths[i];
                string typeName = manifest.Types[i];

                Type type = Types.GetTypeOfName(typeName);

                if (type == null)
                {
                    this.Warning($"Resource manifest at path \"{targetPath}\" has an unknown type \"{typeName}\"");
                    continue;
                }

                if (!_typesToResources.TryGetValue(type, out var entries))
                {
                    entries = [];
                    _typesToResources[type] = entries;
                }

                entries.Add(new()
                {
                    ModId = modId,
                    Path = path,
                    Type = type
                });
            }

            return true;
        }

        bool ISystem.Initialize()
        {
            var mods = ModLoader.Singleton.AllMods;

            foreach (var mod in mods)
            {
                if (!InitializeMod(mod.Value.ModId))
                {
                    return false;
                }
            }

            return true;
        }

        void ISystem.Deinitialize()
        {

        }
    }
}
