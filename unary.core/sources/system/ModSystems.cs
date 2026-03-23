using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    public partial class ModSystems : Node, ICoreSystem
    {
        private readonly SystemCollector<IModSystem> _systems = new();

        public T GetSystem<T>() where T : IModSystem
        {
            return (T)_systems.GetSystem(typeof(T));
        }

        bool ISystem.Initialize()
        {
            HashSet<string> enabledNamespaces = [];

            var sorted = ModLoader.Singleton.EnabledMods;

            foreach (var mod in sorted)
            {
                enabledNamespaces.Add(mod.ModId.ToLower());
            }

            return _systems.Initialize(enabledNamespaces, Bootstrap.Singleton) && _systems.PostInitialize();
        }

        void ISystem.Process(float delta)
        {
            _systems.Process(delta);
        }

        void ISystem.PhysicsProcess(float delta)
        {
            _systems.PhysicsProcess(delta);
        }

        void ISystem.Deinitialize()
        {
            _systems.Deinitialize();
        }
    }
}
