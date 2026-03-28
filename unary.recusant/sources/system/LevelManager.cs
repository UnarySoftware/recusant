using System;
using System.Collections.Generic;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelManager : Node, IModSystem
    {
        private readonly Dictionary<string, LevelDefinition> _levels = [];

        public LevelDefinition CurrentLevel { get; private set; }
        public LevelRoot LevelRoot { get; private set; }

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
            List<LevelDefinition> levels = ResourceTypesManager.Singleton.LoadResourcesOfType<LevelDefinition>(true);

            foreach (var level in levels)
            {
                _levels[level.Name] = level;
                this.Log(level.Name);
            }
            return true;
        }

        bool ISystem.PostInitialize()
        {
            LoadLevel("Quarry");
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

            LevelRoot?.QueueFree();

            _loadDefinition = definition;

            LoadingManager.Singleton.ShowLoading(typeof(UiGameplayState));
            LoadingManager.Singleton.AddJob($"Loading {definition.Name}", GetProgress);

            Resources.Singleton.LoadPatchedAsync(_loadDefinition.Scene.TargetValue, OnLoaded, OnProgress, nameof(PackedScene));
        }

        private void OnLoaded(Resource resource, object data)
        {
            if (resource == null)
            {
                CurrentLevel = null;
                this.Error($"Failed to load a level \"{_loadDefinition.Name}\"");
                return;
            }

            PackedScene scene = (PackedScene)resource;
            LevelRoot levelRoot = (LevelRoot)scene.Instantiate();

            AddChild(levelRoot);

            LevelRoot = levelRoot;
            CurrentLevel = _loadDefinition;

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
