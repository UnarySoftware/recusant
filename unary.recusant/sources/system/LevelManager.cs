using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelManager : Node, IModSystem
    {
        private readonly Dictionary<string, LevelDefinition> _levels = [];

        public LevelRoot Root { get; private set; }
        public LevelDefinition Definition { get; private set; }

        public struct LevelInfo
        {
            public LevelRoot Root;
            public LevelDefinition Definition;
        }

        public EventFunc<LevelInfo> OnLoaded { get; private set; } = new();
        public EventFunc<LevelInfo> OnUnloaded { get; private set; } = new();

        public LevelDefinition GetDefinition(string levelName)
        {
            if (_levels.TryGetValue(levelName, out var result))
            {
                return result;
            }

            return null;
        }

        bool ISystem.Initialize()
        {
            List<LevelDefinition> levels = ResourceTypesManager.Singleton.LoadResourcesOfType<LevelDefinition>();

            foreach (var level in levels)
            {
                _levels[level.Name] = level;
            }

            OnLoaded.Subscribe(OnLevelLoaded, this);
            OnUnloaded.Subscribe(OnLevelUnloaded, this);

            return true;
        }

        private bool OnLevelLoaded(ref LevelInfo data)
        {
            EntityManager.Singleton.Initialize(Entity.EntityType.Level);
            return true;
        }

        private bool OnLevelUnloaded(ref LevelInfo data)
        {
            EntityManager.Singleton.Deinitialize(Entity.EntityType.Level);
            return true;
        }

        void ISystem.Deinitialize()
        {
            OnLoaded.Unsubscribe(this);
            OnUnloaded.Unsubscribe(this);
        }

        bool ISystem.PostInitialize()
        {
            LoadLevel("Test");
            return true;
        }

        private LevelDefinition _loadDefinition;
        private bool _loading = false;

        public void LoadLevel(string LevelName)
        {
            if (!_levels.TryGetValue(LevelName, out var definition))
            {
                this.Error($"Failed to load an unknown level {LevelName}");
                return;
            }

            if (Root != null)
            {
                OnUnloaded.Publish(new()
                {
                    Root = Root,
                    Definition = Definition,
                });

                Root.QueueFree();
                Root = null;
            }

            _loadDefinition = definition;

            if (_loadDefinition.Background)
            {
                LoadingManager.Singleton.ShowLoading(typeof(UiMainMenuState));
            }
            else
            {
                LoadingManager.Singleton.ShowLoading(typeof(UiGameplayState));
            }


            LoadingManager.Singleton.AddJob($"Loading {definition.Name}", GetProgress);

            Resources.Singleton.LoadPatchedAsync(_loadDefinition.Scene.TargetValue, OnLevelLoaded, OnProgress, nameof(PackedScene));
        }

        private void OnLevelLoaded(Resource resource, object data)
        {
            if (resource == null)
            {
                Definition = null;
                this.Error($"Failed to load a level \"{_loadDefinition.Name}\"");
                return;
            }

            PackedScene scene = (PackedScene)resource;
            LevelRoot levelRoot = (LevelRoot)scene.Instantiate();

            AddChild(levelRoot);

            Root = levelRoot;
            Definition = _loadDefinition;

            Progress = 1.0f;
        }

        public float Progress { get; private set; } = 0.0f;

        public float GetProgress()
        {
            return Progress;
        }

        private void OnProgress(float progress)
        {
            Progress = progress;
        }
    }
}
