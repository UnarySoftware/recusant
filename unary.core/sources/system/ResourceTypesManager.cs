using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace Unary.Core
{
    public partial class ResourceTypesManager : Node, ICoreSystem
    {
        private readonly Dictionary<string, ResourceTypesManifest> _manifests = [];
        public Dictionary<Type, List<string>> TypesToResources { get; private set; } = [];

        private readonly Dictionary<string, Type> _pathToType = [];

        public List<string> GetResourcesOfType(Type type)
        {
            if (TypesToResources.TryGetValue(type, out var result))
            {
                return result;
            }
            return [];
        }

        public List<T> LoadResourcesOfType<T>(bool patched = true) where T : BaseResource
        {
            Type type = typeof(T);

            List<T> result = [];

            List<BaseResource> targets = LoadResourcesOfType(type, patched);

            foreach (var target in targets)
            {
                result.Add((T)target);
            }

            return result;
        }

        public List<BaseResource> LoadResourcesOfType(Type type, bool patched = true)
        {
            List<BaseResource> result = [];

            List<string> paths = GetResourcesOfType(type);

            foreach (var path in paths)
            {
                // This is considered a valid non-error-worthy behaviour because the file
                // could have been deleted by some mod but still be present in a types manifest
                if (!ResourceLoader.Singleton.Exists(path))
                {
                    continue;
                }

                Resource resource;

                if (patched)
                {
                    resource = Resources.Singleton.LoadPatched(path, type.Name);
                }
                else
                {
                    resource = ResourceLoader.Singleton.Load(path, type.Name);
                }

                if (resource == null)
                {
                    continue;
                }

                result.Add((BaseResource)resource);
            }

            return result;
        }

        public Type GetPathType(string path)
        {
            if (_pathToType.TryGetValue(path, out var result))
            {
                return result;
            }
            return typeof(Resource);
        }

        public bool InitializeMod(string modId)
        {
            string targetPath = modId + '/' + modId + ResourceTypesManifest.Extension;

            if (!File.Exists(targetPath))
            {
                this.Critical($"Failed to find manifest file with resource types for a mod \"{modId}\"");
                return false;
            }

            if (_manifests.ContainsKey(modId))
            {
                this.Critical($"There is a duplicate manifest file with resource types for a mod \"{modId}\"");
                return false;
            }

            ResourceTypesManifest manifest = JsonSerializer.Deserialize<ResourceTypesManifest>(File.ReadAllText(targetPath));

            manifest.Paths ??= [];
            manifest.Types ??= [];

            _manifests[modId] = manifest;

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

                if (!TypesToResources.TryGetValue(type, out var entries))
                {
                    entries = [];
                    TypesToResources[type] = entries;
                }

                entries.Add(path);

                _pathToType.Add(path, type);
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
