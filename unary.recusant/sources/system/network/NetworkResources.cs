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
            List<string> resources = [];

            List<Type> types = ResourceTypesManager.Singleton.GetResourceTypesAssignableFrom(typeof(NetworkedResource));

            foreach (var type in types)
            {
                List<ResourceTypesManager.ResourceHandle> targetResources = ResourceTypesManager.Singleton.GetResourceHandlesOfType(type);

                foreach (var entry in targetResources)
                {
                    resources.Add(entry.Path);
                }
            }

            resources.Sort();

            uint index = 0;

            foreach (var resource in resources)
            {
                NetworkedResource networkedResource = (NetworkedResource)ResourceLoader.Singleton.Load(resource);
                networkedResource.NetworkId = index;
                _resources[index] = networkedResource;
                index++;
            }

            return true;
        }
    }
}
