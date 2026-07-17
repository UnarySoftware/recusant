using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiRecusantDebugData : UiUnit<UiRecusantState>
    {
        [UiElement("%TopRight")]
        private VBoxContainer _topRight;

        private static LazyResource<PackedScene> _debugEntry { get; } = new("uid://bylf2shs480wo");

        private Dictionary<string, (ColorRect root, Label name, Label value)> _entries = [];

        public override void Initialize()
        {
            DebugManager.Singleton.EntryChanged.Subscribe(OnEntryChanged, this);

            foreach (var entry in DebugManager.Singleton.Entries)
            {
                DebugManager.EventData newData = new()
                {
                    Added = true,
                    Name = entry.Key
                };
                OnEntryChanged(ref newData);
            }
        }

        public override void Deinitialize()
        {
            DebugManager.Singleton.EntryChanged.Unsubscribe(this);
        }

        private bool OnEntryChanged(ref DebugManager.EventData data)
        {
            if (data.Added)
            {
                ColorRect newEntry = (ColorRect)_debugEntry.Cache.Instantiate();
                Label name = newEntry.GetNode<Label>("%Name");
                name.Text = ' ' + data.Name;
                Label value = newEntry.GetNode<Label>("%Value");
                _topRight.AddChild(newEntry);

                if (_entries.TryGetValue(data.Name, out var entry))
                {
                    entry.root.QueueFree();
                    _entries.Remove(data.Name);
                }

                _entries.Add(data.Name, (newEntry, name, value));
            }
            else
            {
                if (_entries.TryGetValue(data.Name, out var entry))
                {
                    entry.root.QueueFree();
                    _entries.Remove(data.Name);
                }
            }

            return true;
        }

        public override void Process(float delta)
        {
            var debugEntries = DebugManager.Singleton.Entries;

            foreach (var entry in _entries)
            {
                if (debugEntries.TryGetValue(entry.Key, out var func))
                {
                    entry.Value.value.Text = func().ToString();
                }
            }
        }
    }
}
