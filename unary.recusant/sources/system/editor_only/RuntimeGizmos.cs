#if TOOLS

using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class RuntimeGizmos : Node, IModSystem
    {
        private readonly LazyResource<ShaderMaterial> _material = new("uid://5kb2d1tm58yv");

        private readonly Queue<RuntimeGizmo> _freeEntries = [];
        private readonly Dictionary<Node3D, (Queue<RuntimeGizmo> entries, int used)> _busyEntries = [];

        bool ISystem.Initialize()
        {
            _material.Precache();
            return true;
        }

        void ISystem.Deinitialize()
        {

        }

        public void Aquire(Node3D root)
        {
            if (_busyEntries.ContainsKey(root))
            {
                return;
            }
            _busyEntries.Add(root, ([], 0));
            root.UpdateGizmo();
        }

        public RuntimeGizmo GetGizmo(Node3D root)
        {
            RuntimeGizmo result;

            if (!_busyEntries.TryGetValue(root, out var target))
            {
                this.Error($"Tried getting gizmo for a Node3D of type {root.GetType().FullName} without calling Aquire first");
                return null;
            }

            (Queue<RuntimeGizmo> entries, int used) = target;

            if (entries.Count <= used)
            {
                if (_freeEntries.Count == 0)
                {
                    result = new RuntimeGizmo();
                    result.Init(_material.Cache);
                    root.AddChild(result);
                    result.Aquire();
                }
                else
                {
                    result = _freeEntries.Dequeue();
                    result.Reparent(root, false);
                    result.Aquire();
                }
                entries.Enqueue(result);
            }
            else
            {
                result = entries.Dequeue();
                result.Aquire();
                entries.Enqueue(result);
            }

            used++;
            _busyEntries[root] = (entries, used);

            return result;
        }

        public void DrawBegin(Node3D root)
        {
            if (!_busyEntries.TryGetValue(root, out var target))
            {
                return;
            }

            (Queue<RuntimeGizmo> entries, int used) = target;

            foreach (var entry in entries)
            {
                entry.Release();
            }

            used = 0;

            _busyEntries[root] = (entries, used);
        }

        public void DrawEnd(Node3D root)
        {
            // This is a placeholder method to get into a habit of ending the draw
            // Later down the line we might want to do something finalization logic with this
        }

        public void Release(Node3D root)
        {
            if (!_busyEntries.TryGetValue(root, out var target))
            {
                return;
            }

            (Queue<RuntimeGizmo> entries, _) = target;

            foreach (var entry in entries)
            {
                entry.Release();
                entry.Reparent(this, false);
                _freeEntries.Enqueue(entry);
            }

            _busyEntries.Remove(root);
        }
    }
}

#endif
