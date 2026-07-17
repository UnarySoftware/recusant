using Godot;
using System;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class ResourcePatcher : ResourceLoaderDetour
    {
        private readonly Dictionary<long, HashSet<ResourcePatch>> _patches = [];

        public ResourcePatcher()
        {
            List<ResourcePatch> patches = ResourceTypesManager.Singleton.LoadResources<ResourcePatch>(false);

            foreach (var patch in patches)
            {
                if (!BaseSelectorResource.IsValid(patch.Target))
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
        }

        public override string[] _GetRecognizedExtensions()
        {
            return ["res", "tres"];
        }

        public override Resource _OnLoad(Resource resource)
        {
            long id = ResourceLoader.Singleton.GetResourceUid(resource.ResourcePath);

            if (id == ResourceUid.InvalidId)
            {
                return resource;
            }

            if (!_patches.TryGetValue(id, out var patches))
            {
                return resource;
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

            return resource;
        }
    }
}
