#if TOOLS

using Godot;
using System.Collections.Generic;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginDock : IPluginSystem
    {
        private sealed class EditorEntry
        {
            public VBoxContainer Root;
            public int Index;
            public string ModId = string.Empty;
            public string Group = string.Empty;
            public ulong EditorId;
        }

        private readonly Dictionary<string, EditorDock> _docks = [];
        private readonly Dictionary<string, Dictionary<string, GroupEntry>> _modIdToEntries = [];
        private readonly Dictionary<EditorSettingVariableBase, EditorEntry> _editors = [];
        private readonly List<(GodotObject source, StringName signal, Callable callable, GodotObject handler)> _signalConnections = [];

        private struct Entry
        {
            public string Name;
            public Control Control;
        }

        private struct GroupEntry
        {
            public string Name;
            public Control Control;
            public List<Entry> Entries;
        }

        public void Filter(string modId, string text)
        {
            if (!_modIdToEntries.TryGetValue(modId, out var groups))
            {
                return;
            }

            foreach (var group in groups)
            {
                bool groupMatches = text.IsSubsequenceOfN(group.Key);

                bool anyEntryVisible = false;
                foreach (var entry in group.Value.Entries)
                {
                    bool visible = groupMatches || text.IsSubsequenceOfN(entry.Name);
                    entry.Control.Visible = visible;
                    anyEntryVisible |= visible;
                }

                group.Value.Control.Visible = groupMatches || anyEntryVisible;
            }
        }

        public void UpdateInspector(EditorSettingVariableBase variable)
        {
            if (_editors.TryGetValue(variable, out var storage))
            {
                CreateVariable(storage.Root, storage.Index, storage.ModId, storage.Group, variable, new());
            }
        }

        private void CreateVariable(VBoxContainer container, int index, string modId, string group, EditorSettingVariableBase variableBase, GroupEntry groupEntry)
        {
            if (_editors.TryGetValue(variableBase, out var storage))
            {
                if (GodotObject.InstanceFromId(storage.EditorId) is EditorProperty target)
                {
                    target.QueueFree();
                }

                // Use a local variable to make intent explicit and avoid mutation of the copy
                int entryIndex = storage.Index - 1;
                _modIdToEntries[storage.ModId][group].Entries.RemoveAt(entryIndex);
            }

            EditorProperty editor = EditorInspector.InstantiatePropertyEditor(variableBase.Wrapper, variableBase.GetField().VariantType,
            nameof(EditorSettingWrapper.Value), variableBase.PropertyHint, variableBase.HintText, (uint)PropertyUsageFlags.Editor, true);

            if (variableBase.Description != null &&
                !string.IsNullOrEmpty(variableBase.Description))
            {
                editor.TooltipText = variableBase.Description;
            }

            variableBase.Inspector = editor;

            editor.SetObjectAndProperty(variableBase.Wrapper, nameof(EditorSettingWrapper.Value));
            editor.Label = variableBase.Name;

            var handler = new PluginDockVariableHandler();
            handler.Setup(variableBase, editor);
            var callable = new Callable(handler, PluginDockVariableHandler.MethodName.OnPropertyChanged);
            editor.Connect(EditorProperty.SignalName.PropertyChanged, callable);
            _signalConnections.Add((editor, EditorProperty.SignalName.PropertyChanged, callable, handler));

            editor.UpdateProperty();

            container.AddChild(editor);

            handler.CallDeferred(nameof(PluginDockVariableHandler.MethodName.MoveNode), container, editor, index);

            _editors[variableBase] = new()
            {
                Root = container,
                Index = index,
                ModId = modId,
                Group = group,
                EditorId = editor.GetInstanceId()
            };

            groupEntry.Entries?.Add(new Entry
            {
                Name = variableBase.Name,
                Control = editor
            });
        }

        private void InitializeControl(VBoxContainer container, EditorSettingBase entry, int counter, string modId, string group, GroupEntry groupEntry)
        {
            if (entry.Type == EditorSettingType.Variable)
            {
                CreateVariable(container, counter, modId, group, (EditorSettingVariableBase)entry, groupEntry);
            }
            else if (entry.Type == EditorSettingType.Action)
            {
                EditorSettingAction action = (EditorSettingAction)entry;

                Button newButton = new()
                {
                    Text = entry.Name,
                };

                if (action.Description != null &&
                    !string.IsNullOrEmpty(action.Description))
                {
                    newButton.TooltipText = action.Description;
                }

                var handler = new PluginDockActionHandler();
                handler.Setup(action);
                var callable = new Callable(handler, PluginDockActionHandler.MethodName.OnPressed);
                newButton.Connect(BaseButton.SignalName.Pressed, callable);
                _signalConnections.Add((newButton, BaseButton.SignalName.Pressed, callable, handler));

                container.AddChild(newButton);

                groupEntry.Entries.Add(new()
                {
                    Name = entry.Name,
                    Control = newButton,
                });
            }
        }

        private void InitializeGroups(EditorPlugin plugin)
        {
            Dictionary<string, Dictionary<string, List<EditorSettingBase>>> sorted = [];

            foreach (var entry in EditorSettingManager.GetEntries())
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

                LineEdit lineEdit = new()
                {
                    PlaceholderText = "Search..."
                };
                dockEntries.AddChild(lineEdit);
                dockEntries.GrowHorizontal = Control.GrowDirection.Both;

                if (!_modIdToEntries.TryGetValue(modId.Key, out var entries))
                {
                    entries = [];
                    _modIdToEntries[modId.Key] = entries;
                }

                PluginDockFilterHandler handler = new();
                handler.Setup(this, modId.Key);

                var callable = new Callable(handler, PluginDockFilterHandler.MethodName.OnTextChanged);
                lineEdit.Connect(LineEdit.SignalName.TextChanged, callable);
                _signalConnections.Add((lineEdit, LineEdit.SignalName.TextChanged, callable, handler));

                foreach (var group in modId.Value)
                {
                    VBoxContainer groupContainer = new();
                    dockEntries.AddChild(groupContainer);

                    string groupName = group.Key;

                    GroupEntry groupEntry = new()
                    {
                        Name = groupName,
                        Control = groupContainer,
                        Entries = []
                    };

                    Label groupLabel = new()
                    {
                        Text = group.Key,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    groupContainer.AddChild(groupLabel);

                    // We start from 1 since index 0 is reserved for the label representing the groups name
                    int counter = 1;

                    foreach (var entry in group.Value)
                    {
                        InitializeControl(groupContainer, entry, counter, modId.Key, groupName, groupEntry);
                        counter++;
                    }

                    entries.Add(groupName, groupEntry);
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
            foreach (var (source, signal, callable, _) in _signalConnections)
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
            _modIdToEntries.Clear();
        }
    }
}

#endif
