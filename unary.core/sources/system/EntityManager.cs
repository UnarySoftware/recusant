using System.Collections.Generic;
using Godot;
using Unary.Core;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class EntityManager : Node, ICoreSystem
    {
        private readonly Dictionary<ushort, Entity> _levelEntities = [];
        private readonly Dictionary<ushort, Entity> _pooledEntities = [];
        private ushort _idCounter = 0;
        private ushort _lastPooledId = 0;

        public void Add(Entity entity)
        {
            switch (entity.Type)
            {
                case Entity.EntityType.Level:
                    {
                        _levelEntities[_idCounter] = entity;
                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        _pooledEntities[_idCounter] = entity;
                        break;
                    }
            }

            entity.Id = _idCounter;
            _idCounter++;
        }

        public void FinishedPooling()
        {
            _lastPooledId = _idCounter;
        }

        public void Initialize(Entity.EntityType type)
        {
            switch (type)
            {
                case Entity.EntityType.Level:
                    {
                        foreach (var entity in _levelEntities)
                        {
                            entity.Value.Initialize();
                        }
                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        foreach (var entity in _pooledEntities)
                        {
                            entity.Value.Initialize();
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
                            entity.Value.Deinitialize();
                        }

                        _levelEntities.Clear();
                        _idCounter = _lastPooledId;

                        break;
                    }
                case Entity.EntityType.Pooled:
                    {
                        foreach (var entity in _pooledEntities)
                        {
                            entity.Value.Deinitialize();
                        }
                        break;
                    }
            }
        }
    }
}
