#if TOOLS

using Godot;
using System.Collections.Generic;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginBridgeHostVariables : IPluginSystem
    {
        private Dictionary<int, EditorSettingVariableBase> _entries = [];

        public static Dictionary<int, EditorSettingVariableBase> GetEntries()
        {
            Dictionary<int, EditorSettingVariableBase> result = [];

            var entries = EditorSettingManager.GetEntries();

            foreach (var entry in entries)
            {
                if (entry.Type != EditorSettingType.Variable)
                {
                    continue;
                }

                EditorSettingVariableBase variable = (EditorSettingVariableBase)entry;

                result.Add(variable.Hash, variable);
            }

            return result;
        }

        bool ISystem.Initialize()
        {
            PluginBridgeHost.OnConnected += OnConnected;

            _entries = GetEntries();

            foreach (var entry in _entries)
            {
                entry.Value.OnValueChanged += OnChanged;
            }

            return true;
        }

        void ISystem.Deinitialize()
        {
            PluginBridgeHost.OnConnected -= OnConnected;

            foreach (var entry in _entries)
            {
                entry.Value.OnValueChanged -= OnChanged;
            }
        }

        private void OnConnected(StreamPeerTcp peer)
        {
            foreach (var entry in _entries)
            {
                if (entry.Value.IsDefault())
                {
                    continue;
                }

                EditorVariablePacket newPacket = new()
                {
                    Value = entry.Value.GetField(),
                    VariableHash = entry.Key
                };

                PluginBridgeHost.Singleton.Send(newPacket, [peer]);
            }
        }

        private void OnChanged(EditorSettingVariableBase variable)
        {
            int hash = variable.Path.GetDeterministicHashCode();

            EditorVariablePacket newPacket = new()
            {
                Value = variable.GetField(),
                VariableHash = hash
            };

            PluginBridgeHost.Singleton.Send(newPacket, null);
        }
    }
}

#endif
