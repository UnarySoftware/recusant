using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    public partial class NetworkResources : Node, IModSystem
    {
        private readonly Dictionary<uint, NetworkedResource> _resources = [];

        public T GetResource<T>(uint Id) where T : NetworkedResource
        {
            if (_resources.TryGetValue(Id, out var result))
            {
                return (T)result;
            }
            return null;
        }

        bool ISystem.Initialize()
        {
            Type networkResource = typeof(NetworkedResource);

            List<string> resources = [];

            foreach (var manifest in ResourceTypesManager.Singleton.TypesToResources)
            {
                if (networkResource.IsAssignableFrom(manifest.Key))
                {
                    foreach (var entry in manifest.Value)
                    {
                        resources.Add(entry);
                    }
                }
            }

            resources.Sort();

            uint index = 0;

            foreach (var resource in resources)
            {
                NetworkedResource networkedResource = (NetworkedResource)Resources.Singleton.LoadPatched(resource);
                networkedResource.NetworkId = index;
                _resources[index] = networkedResource;
                index++;
            }

            return true;
        }
    }
}
