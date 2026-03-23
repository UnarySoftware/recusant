using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioManager : Node, ICoreSystem
    {
        private Dictionary<string, int> _buses = [];

        public int GetBusIndex(string busName)
        {
            if (_buses.TryGetValue(busName, out var result))
            {
                return result;
            }

            return -1;
        }

        bool ISystem.Initialize()
        {
            List<AudioBusDeclaration> buses = ResourceTypesManager.Singleton.LoadResourcesOfType<AudioBusDeclaration>();

            var server = AudioServer.Singleton;

            // Master bus should be on 50% by default
            server.SetBusVolumeLinear(0, 0.5f);

            int index = 1;

            // TODO add bus patching
            foreach (var bus in buses)
            {
                server.AddBus(index);
                server.SetBusName(index, bus.Name);

                if (bus.Parent == null || string.IsNullOrEmpty(bus.Parent.Name))
                {
                    server.SetBusSend(index, "Master");
                }
                else
                {
                    server.SetBusSend(index, bus.Parent.Name);
                }

                server.SetBusVolumeLinear(index, bus.Volume);
                _buses[bus.Name] = index;
                index++;
            }

            return true;
        }
    }
}
