using Godot;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class AudioManager : Node, ICoreSystem
    {
        public const string MasterBusName = "Master";

        public Dictionary<string, int> Buses { get; private set; } = [];

        bool ISystem.Initialize()
        {
            List<AudioBusDeclaration> buses = ResourceTypesManager.Singleton.LoadResourcesOfType<AudioBusDeclaration>();

            var server = AudioServer.Singleton;

            // Master bus should be on 50% by default
            server.SetBusVolumeLinear(0, 0.5f);

            int index = 1;

            foreach (var bus in buses)
            {
                server.AddBus(index);
                server.SetBusName(index, bus.Name);

                if (bus.Parent == null || string.IsNullOrEmpty(bus.Parent.Name))
                {
                    server.SetBusSend(index, MasterBusName);
                }
                else
                {
                    server.SetBusSend(index, bus.Parent.Name);
                }

                server.SetBusVolumeLinear(index, bus.Volume);
                Buses[bus.Name] = index;
                index++;
            }

            Buses[MasterBusName] = 0;

            return true;
        }
    }
}
