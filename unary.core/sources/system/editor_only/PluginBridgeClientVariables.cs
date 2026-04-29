#if TOOLS

using Godot;
using System.Collections.Generic;
using Unary.Core.Editor;

namespace Unary.Core
{
    [Tool]
    public partial class PluginBridgeClientVariables : Node, ICoreSystem
    {
        private Dictionary<int, EditorSettingVariableBase> _entries;

        bool ISystem.Initialize()
        {
            IStreamSerializable<EditorVariablePacket>.OnRecieve += OnRecieve;

            _entries = PluginBridgeHostVariables.GetEntries();

            return true;
        }

        void ISystem.Deinitialize()
        {
            IStreamSerializable<EditorVariablePacket>.OnRecieve -= OnRecieve;
        }

        private void OnRecieve(EditorVariablePacket packet)
        {
            if (!_entries.TryGetValue(packet.VariableHash, out var result))
            {
                this.Error($"Recieved an unknown packet with hash {packet.VariableHash} over the plugin bridge from host");
                return;
            }

            result.VariantValue = packet.Value;
        }
    }
}

#endif
