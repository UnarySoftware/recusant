using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class DebugManager : Node, IModSystem
    {
        public Dictionary<string, Func<Variant>> Entries { get; private set; } = [];

        public struct EventData
        {
            public string Name;
            public bool Added;
        }

        public EventFunc<EventData> EntryChanged = new();

        bool ISystem.Initialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {

        }

        public void Add(string name, Func<Variant> getter)
        {
            Entries[name] = getter;
            EntryChanged.Publish(new()
            {
                Name = name,
                Added = true
            });
        }

        public void Remove(string name)
        {
            Entries.Remove(name);
            EntryChanged.Publish(new()
            {
                Name = name,
                Added = false
            });
        }
    }
}
