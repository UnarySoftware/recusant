using System.Collections.Generic;
using Godot;
using Unary.Core;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class EntityManager : Node, IModSystem
    {
        private readonly HashSet<Entity> _levelEntities = [];
        private readonly HashSet<Entity> _pooledEntities = [];

        public void Add(Entity entity)
        {
            switch (entity.Type)
            {
                case Entity.EntityType.Level:
                    {
                        _levelEntities.Add(entity);
                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        _pooledEntities.Add(entity);
                        break;
                    }
            }
        }

        public void Initialize(Entity.EntityType type)
        {
            switch (type)
            {
                case Entity.EntityType.Level:
                    {
                        foreach (var entity in _levelEntities)
                        {
                            entity.Initialize();
                        }
                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        foreach (var entity in _pooledEntities)
                        {
                            entity.Initialize();
                        }
                        break;
                    }
            }
        }

        public void Deinitialize(Entity.EntityType type)
        {
            switch (type)
            {
                case Entity.EntityType.Level:
                    {
                        foreach (var entity in _levelEntities)
                        {
                            entity.Deinitialize();
                        }

                        _levelEntities.Clear();

                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        foreach (var entity in _pooledEntities)
                        {
                            entity.Deinitialize();
                        }
                        break;
                    }
            }
        }
    }
}
