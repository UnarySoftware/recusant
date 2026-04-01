using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PoolManager : Node, IModSystem
    {
        private Dictionary<string, PoolGroup> _groups = [];

        public PoolGroup GetGroup(string poolId)
        {
            if (_groups.TryGetValue(poolId, out var result))
            {
                return result;
            }
            this.Error($"Failed to find a pool group for id \"{poolId}\")");
            return null;
        }

        bool ISystem.Initialize()
        {
            List<PoolDeclaration> pools = ResourceTypesManager.Singleton.LoadResourcesOfType<PoolDeclaration>();

            Dictionary<string, PoolDeclaration> poolDictionary = [];

            foreach (var pool in pools)
            {
                poolDictionary[ResourceUid.PathToUid(pool.ResourcePath)] = pool;
            }

            Dictionary<string, (int changedCount, HashSet<string> influencers)> changedCounts = [];

            foreach (var pool in poolDictionary)
            {
                // This pool does not influence any count, skip
                if (pool.Value.InfluencedCount == null || pool.Value.InfluencedCount.Count == 0)
                {
                    continue;
                }

                foreach (var influence in pool.Value.InfluencedCount)
                {
                    if (!poolDictionary.TryGetValue(influence.Key.TargetValue, out var targetPool))
                    {
                        RuntimeLogger.Error(this, $"Pool with scene \"{pool.Value.Scene.TargetValue}\" just tried influencing unknown pool \"{influence.Key.TargetValue}\"");
                        continue;
                    }

                    int newCount = targetPool.Count * influence.Value;

                    if (!changedCounts.TryGetValue(influence.Key.TargetValue, out var influencedPool))
                    {
                        influencedPool = new()
                        {
                            changedCount = targetPool.Count,
                            influencers = []
                        };
                        changedCounts[influence.Key.TargetValue] = influencedPool;
                    }

                    if (influencedPool.influencers.Contains(pool.Key))
                    {
                        continue;
                    }

                    if (influencedPool.changedCount > newCount)
                    {
                        continue;
                    }

                    influencedPool.changedCount = newCount;
                    influencedPool.influencers.Add(pool.Key);
                }
            }

            foreach (var changed in changedCounts)
            {
                poolDictionary[changed.Key].Count = changed.Value.changedCount;
            }

            foreach (var pool in poolDictionary)
            {
                _groups[pool.Key] = new(pool.Value.Count, pool.Key, this, pool.Value.Scene.LoadWithoutCache<PackedScene>());
            }

            EntityManager.Singleton.Initialize(Entity.EntityType.Pooled);

            return true;
        }

        void ISystem.Deinitialize()
        {
            EntityManager.Singleton.Deinitialize(Entity.EntityType.Pooled);
        }
    }
}
