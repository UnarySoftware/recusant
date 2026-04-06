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

        private readonly Queue<RuntimeGizmo> _entries = [];

        bool ISystem.Initialize()
        {
            _material.Precache();

            return true;
        }

        void ISystem.Deinitialize()
        {

        }

        public RuntimeGizmo Aquire()
        {
            RuntimeGizmo result;

            if (_entries.Count == 0)
            {
                result = new(this, _material.Cache);
                return result;
            }
            else
            {
                result = _entries.Dequeue();
                result.Aquire();
                return result;
            }
        }

        public void Release(RuntimeGizmo gizmo)
        {
            gizmo.Release();
            _entries.Enqueue(gizmo);
        }
    }
}

#endif
