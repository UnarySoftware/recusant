using System;
using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass, Icon("res://addons/unary.core.editor/icons/Entity.svg")]
    public partial class Entity : Node
    {
        private readonly Dictionary<Type, Component> _typeCache = [];
        private readonly List<Component> _componentCache = [];
        private bool _initialized = false;

        public override void _Ready()
        {

        }

        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            var children = GetChildren();

            foreach (var child in children)
            {
                if (child is Component component)
                {
                    _typeCache.Add(child.GetType(), component);
                    _componentCache.Add(component);
                }
            }

            _initialized = true;
        }

        public T GetComponent<T>() where T : Component
        {
            TryInitialize();

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
    }
}
