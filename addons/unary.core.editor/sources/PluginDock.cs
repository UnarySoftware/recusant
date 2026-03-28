#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginDock : IPluginSystem
    {
        private Dictionary<string, EditorDock> _docks = [];
        private Dictionary<EditorSettingVariableBase, VBoxContainer> _editors = [];
        private readonly List<(GodotObject source, StringName signal, Callable callable)> _signalConnections = [];

        public void UpdateInspector(EditorSettingVariableBase variable)
        {
            if (_editors.TryGetValue(variable, out var storage))
            {
                UpdateVariable(storage, variable);
            }
        }

        private void UpdateVariable(VBoxContainer container, EditorSettingVariableBase variableBase)
        {
            if (_editors.TryGetValue(variableBase, out var storage))
            {
                storage.GetChild(1).QueueFree();
            }

            _editors[variableBase] = container;

            EditorProperty editor = EditorInspector.InstantiatePropertyEditor(variableBase.Wrapper, variableBase.VariantValue.VariantType,
            nameof(EditorSettingWrapper.Value), variableBase.PropertyHint,
            variableBase.HintText, (uint)PropertyUsageFlags.Editor, true);

            variableBase.Inspector = editor;

            editor.SetObjectAndProperty(variableBase.Wrapper, nameof(EditorSettingWrapper.Value));
            editor.Label = variableBase.Name;

            var callable = Callable.From((StringName property, Variant value, StringName field, bool changing) =>
            {
                variableBase.Wrapper.Value = value;
                editor.UpdateProperty();
            });
            editor.Connect(EditorProperty.SignalName.PropertyChanged, callable);
            _signalConnections.Add((editor, EditorProperty.SignalName.PropertyChanged, callable));

            editor.UpdateProperty();

            container.AddChild(editor);
        }

        private void InitializeControl(VBoxContainer container, EditorSettingBase entry)
        {
            if (entry.Type == EditorSettingType.Variable)
            {
                UpdateVariable(container, (EditorSettingVariableBase)entry);
            }
            else if (entry.Type == EditorSettingType.Action)
            {
                EditorSettingAction action = (EditorSettingAction)entry;
                Button newButton = new();
                newButton.Text = entry.Name;

                var callable = Callable.From(() =>
                {
                    action.MethodInfo.Invoke(null, null);
                });
                newButton.Connect(BaseButton.SignalName.Pressed, callable);
                _signalConnections.Add((newButton, BaseButton.SignalName.Pressed, callable));

                container.AddChild(newButton);
            }
        }

        private void InitializeGroups(EditorPlugin plugin)
        {
            Dictionary<string, Dictionary<string, List<EditorSettingBase>>> sorted = [];

            foreach (var entry in EditorSettingsManager.GetEntries())
            {
                if (!sorted.TryGetValue(entry.ModId, out var groups))
                {
                    groups = [];
                    sorted.Add(entry.ModId, groups);
                }

                if (!groups.TryGetValue(entry.Group, out var entries))
                {
                    entries = [];
                    groups.Add(entry.Group, entries);
                }

                entries.Add(entry);
            }

            foreach (var modId in sorted)
            {
                VBoxContainer dockEntries = new();
                dockEntries.SetAnchorsPreset(Control.LayoutPreset.FullRect);

                foreach (var group in modId.Value)
                {
                    VBoxContainer groupContainer = new();
                    dockEntries.AddChild(groupContainer);

                    Label groupLabel = new();
                    groupContainer.AddChild(groupLabel);
                    groupLabel.Text = group.Key;
                    groupLabel.HorizontalAlignment = HorizontalAlignment.Center;

                    foreach (var entry in group.Value)
                    {
                        InitializeControl(groupContainer, entry);
                    }
                }

                EditorDock editorDock = new()
                {
                    Title = modId.Key.Replace('.', ' '),
                    DefaultSlot = EditorDock.DockSlot.RightUl
                };
                editorDock.AddChild(dockEntries);

                plugin.AddDock(editorDock);

                _docks.Add(modId.Key, editorDock);
            }
        }

        bool ISystem.PostInitialize()
        {
            InitializeGroups(this.GetPlugin());
            return true;
        }

        void ISystem.Deinitialize()
        {
            foreach (var (source, signal, callable) in _signalConnections)
            {
                if (GodotObject.IsInstanceValid(source) && source.IsConnected(signal, callable))
                {
                    source.Disconnect(signal, callable);
                }
            }
            _signalConnections.Clear();

            var plugin = this.GetPlugin();

            foreach (var dock in _docks)
            {
                plugin.RemoveDock(dock.Value);
                dock.Value.Free();
            }

            _docks.Clear();
            _editors.Clear();
        }
    }
}
#endif
