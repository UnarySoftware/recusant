using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class StaticWebDataFetcher : Node, IModSystem
    {
        private readonly Dictionary<Type, BaseResource> _resources = [];

        public struct RecievedData
        {
            public Type Type;
            public BaseResource Resource;
        }

        public EventFunc<RecievedData> OnRecievedData { get; } = new();

        public T GetData<T>() where T : BaseResource
        {
            Type type = typeof(T);
            if (!_resources.TryGetValue(type, out var result))
            {
                this.Error($"Tried fetching data for unknown type {type.FullName}");
                return null;
            }
            return (T)result;
        }

        private static readonly System.Net.Http.HttpClient httpClient = new();

        private async Task FetchLatest(Type type)
        {
            StaticWebDataAttribute attribute = Types.GetTypeAttribute<StaticWebDataAttribute>(type);

            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(attribute.Path);
            }
            catch (Exception)
            {
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            string fileName = Path.GetTempPath() + type.FullName + ".tres";

            await File.WriteAllTextAsync(fileName, content);

            BaseResource resource = (BaseResource)ResourceLoader.Singleton.Load(fileName, type.Name);

            File.Delete(fileName);

            _resources[type] = resource;

            OnRecievedData.Publish(new()
            {
                Type = type,
                Resource = resource
            });
        }

        bool ISystem.Initialize()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(7.0);

            var types = Types.GetTypesWithAttribute(typeof(StaticWebDataAttribute));

            ResourceTypesManager resources = ResourceTypesManager.Singleton;

            foreach (var type in types)
            {
                List<BaseResource> targetResources = resources.LoadResourcesOfType(type);

                if (targetResources.Count == 0)
                {
                    this.Warning($"There is a resource using a {typeof(StaticWebDataAttribute).FullName} attribute but no instance of it exists");
                    continue;
                }

                _resources[type] = targetResources[0];

                _ = FetchLatest(type);
            }

            return true;
        }

        void ISystem.Deinitialize()
        {

        }
    }
}
