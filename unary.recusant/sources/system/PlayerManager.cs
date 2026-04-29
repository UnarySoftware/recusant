using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PlayerManager : Node, IModSystem
    {
        private Dictionary<PlayerMarker.MarkerType, (int index, List<PlayerMarker> entries)> _markers = [];

        private readonly LazyResource<PoolDeclaration> _playerPoolHandle = new("uid://c16iylkg5pp3i");

        private readonly HashSet<Entity> _players = [];

        private PoolGroup _playerPool;


        bool ISystem.Initialize()
        {
            _playerPool = PoolManager.Singleton.GetGroup(_playerPoolHandle.TargetValue);

            LevelManager.Singleton.OnLoaded.Subscribe(OnLoaded, this);
            LevelManager.Singleton.OnUnloaded.Subscribe(OnUnloaded, this);

            return true;
        }

        void ISystem.Deinitialize()
        {
            LevelManager.Singleton.OnLoaded.Unsubscribe(this);
            LevelManager.Singleton.OnUnloaded.Unsubscribe(this);
        }

        private bool OnLoaded(ref LevelManager.LevelInfo info)
        {
            Entity entity = _playerPool.Aquire<Entity>(true);

            entity.GetComponent<Player>().Body.GlobalPosition = GetMarker(PlayerMarker.MarkerType.Start).GlobalPosition;

            _players.Add(entity);
            return true;
        }

        private bool OnUnloaded(ref LevelManager.LevelInfo info)
        {
            foreach (var player in _players)
            {
                _playerPool.Release(player);
            }

            _players.Clear();
            _markers.Clear();

            return true;
        }

        public PlayerMarker GetMarker(PlayerMarker.MarkerType type)
        {
            if (!_markers.TryGetValue(type, out var entries))
            {
                this.Error($"Failed to return any player markers of type \"{type}\"");
                return null;
            }

            PlayerMarker result = entries.entries[entries.index];

            entries.index++;

            if (entries.index == _markers.Count)
            {
                entries.index = 0;
            }

            return result;
        }

        public void AddMarker(PlayerMarker marker)
        {
            if (!_markers.TryGetValue(marker.Type, out var markers))
            {
                markers = (0, []);
                _markers[marker.Type] = markers;
            }

            markers.entries.Add(marker);
        }

        public void RemoveMarker(PlayerMarker marker)
        {
            if (_markers.TryGetValue(marker.Type, out var markers))
            {
                markers.entries.Remove(marker);
            }
        }
    }
}
