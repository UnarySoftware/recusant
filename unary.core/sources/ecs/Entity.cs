using Godot;
using System;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Entity.svg")]
    public partial class Entity : Node3D, IPoolable
    {
        public enum EntityType
        {
            Level,
            Pooled
        };

        [Export]
        public EntityType Type;

        [Flags]
        public enum EntityFlags
        {
            None = 0,
            Networked = 1 << 0,
            PlayerControlled = Networked | (1 << 1)
        };

        [Export]
        public EntityFlags Flags = EntityFlags.None;

        public ushort Id = 0;

        private readonly Dictionary<Type, Component> _typeCache = [];
        private readonly List<Component> _componentCache = [];

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            EntityManager.Singleton.Add(this);
        }

        public void Initialize()
        {
            var children = GetChildren();

            foreach (var child in children)
            {
                if (child is Component component)
                {
                    _typeCache.Add(child.GetType(), component);
                    _componentCache.Add(component);
                }
            }

            foreach (var component in _componentCache)
            {
                component.Initialize();
            }
        }

        public void Deinitialize()
        {
            foreach (var component in _componentCache)
            {
                component.Deinitialize();
            }
        }

        public T GetComponent<T>() where T : Component
        {
            Type type = typeof(T);

            if (_typeCache.TryGetValue(type, out var entry))
            {
                return (T)entry;
            }
            else
            {
                foreach (var component in _componentCache)
                {
                    if (component is T target)
                    {
                        _typeCache[type] = component;
                        return target;
                    }
                }
            }

            return null;
        }

        public static Entity Find(Node node)
        {
            if (node is Entity baseEntity)
            {
                return baseEntity;
            }
            else if (node is BrushEntity brushEntity)
            {
                return brushEntity.Entity;
            }

            Node parent = node.GetParent();

            while (true)
            {
                if (parent == null)
                {
                    break;
                }
                else if (parent is Entity entity)
                {
                    return entity;
                }
                else if (parent is BrushEntity brushEntity)
                {
                    return brushEntity.Entity;
                }
                parent = parent.GetParent();
            }

            RuntimeLogger.Warning(typeof(Entity), $"{nameof(Entity)}.{nameof(Find)} failed to find anything from {node.GetPath()}");

            return null;
        }

        public void Aquire()
        {
            foreach (var component in _componentCache)
            {
                if (component is IPoolable poolable)
                {
                    poolable.Aquire();
                }
            }
        }

        public void Release()
        {
            foreach (var component in _componentCache)
            {
                if (component is IPoolable poolable)
                {
                    poolable.Release();
                }
            }
        }
    }
}
