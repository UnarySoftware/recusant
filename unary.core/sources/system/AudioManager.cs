using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioManager : Node, ICoreSystem
    {
        public const string MasterBusName = "Master";

        public Dictionary<string, int> Buses { get; private set; } = [];
        private readonly Dictionary<string, Dictionary<string, int>> _modIdToBusData = [];

        private List<AudioBusDeclaration> _buses = [];

        bool ISystem.Initialize()
        {
            _buses = ResourceTypesManager.Singleton.LoadResources<AudioBusDeclaration>();

            var server = AudioServer.Singleton;

            // Master bus should be on 50% by default
            server.SetBusVolumeLinear(0, 0.5f);

            int index = 1;

            foreach (var bus in _buses)
            {
                server.AddBus(index);
                server.SetBusName(index, bus.Name);

                AudioBusDeclaration parent = null;

                if (bus.Parent != null)
                {
                    parent = bus.Parent.Load<AudioBusDeclaration>();
                }

                if (parent == null || string.IsNullOrEmpty(parent.Name))
                {
                    server.SetBusSend(index, MasterBusName);
                }
                else
                {
                    server.SetBusSend(index, parent.Name);
                }

                server.SetBusVolumeLinear(index, bus.Volume / 100.0f);
                Buses[bus.Name] = index;

                if (!_modIdToBusData.TryGetValue(bus.ModId, out var entries))
                {
                    entries = [];
                    _modIdToBusData.Add(bus.ModId, entries);
                }

                entries[bus.Name] = index;
                index++;
            }

            Buses[MasterBusName] = 0;

            string modId = this.GetModId().ToLower();

            if (!_modIdToBusData.TryGetValue(modId, out var coreEntries))
            {
                coreEntries = [];
                _modIdToBusData.Add(modId, coreEntries);
            }

            coreEntries[MasterBusName] = 0;

            Load();

            return true;
        }

        void ISystem.Deinitialize()
        {
            Save();
        }

        void Save()
        {
            foreach (var modId in _modIdToBusData)
            {
                List<AudioBusSerializable> data = [];

                foreach (var busEntry in modId.Value)
                {
                    data.Add(new()
                    {
                        Name = busEntry.Key,
                        Volume = Mathf.Clamp(AudioServer.Singleton.GetBusVolumeLinear(busEntry.Value), 0.0f, 1.0f)
                    });
                }

                StorageManager.Singleton.WriteEntryText(modId.Key, nameof(AudioManager), JsonSerializer.Serialize(data, JsonConverters.IndentedOptions));
            }
        }

        void Load()
        {
            foreach (var modId in _modIdToBusData)
            {
                string content = StorageManager.Singleton.ReadEntryText(modId.Key, nameof(AudioManager));

                if (content == string.Empty)
                {
                    continue;
                }

                List<AudioBusSerializable> data = JsonSerializer.Deserialize<List<AudioBusSerializable>>(content, JsonConverters.IndentedOptions);

                foreach (var busEntry in data)
                {
                    if (!modId.Value.TryGetValue(busEntry.Name, out var busIndex))
                    {
                        this.Warning($"Tried setting an unknown bus \"{busEntry.Name}\" when loading audio data for \"{modId.Key}\", skipping...");
                        continue;
                    }

                    AudioServer.Singleton.SetBusVolumeLinear(busIndex, Mathf.Clamp(busEntry.Volume, 0.0f, 1.0f));
                }
            }
        }
    }
}
