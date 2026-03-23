#if TOOLS

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginBootstrap : EditorPlugin
    {
        public static PluginBootstrap Singleton { get; private set; } = null;
        public bool Debug { get; private set; } = false;

        private SystemCollector<IPluginSystem> _systems = new();

        public T GetSystem<T>() where T : IPluginSystem
        {
            Type type = typeof(T);
            return (T)_systems.GetSystem(type);
        }

        private static HashSet<string> GetEnabledPlugins()
        {
            HashSet<string> result = ["unary.core.editor"];

            ConfigFile configFile = new();

            if (configFile.Load("project.godot") != Error.Ok)
            {
                return result;
            }

            var value = configFile.GetValue("editor_plugins", "enabled");

            if (value.VariantType != Variant.Type.PackedStringArray)
            {
                return result;
            }

            string[] plugins = value.As<string[]>();

            foreach (var plugin in plugins)
            {
                ConfigFile pluginFile = new();
                if (pluginFile.Load(plugin) != Error.Ok)
                {
                    continue;
                }

                result.Add(pluginFile.GetValue("plugin", "name", "Unknown").AsString().ToLower());
            }

            return result;
        }

        public HashSet<string> EnabledPlugins { get; private set; } = null;

        public void Initialize()
        {
            Singleton = this;

            PluginLogger.Initialize();

            Types.Initialize(PluginLogger.Critical);

            EnabledPlugins = GetEnabledPlugins();

            if (!_systems.Initialize(EnabledPlugins, null) || !_systems.PostInitialize())
            {
                Task.Run(() => TryAbortingInitialization());
            }
        }

        public async Task TryAbortingInitialization()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (_systems.Initialized && EditorInterface.Singleton.IsPluginEnabled("unary.core.editor"))
            {
                EditorInterface.Singleton.SetPluginEnabled("unary.core.editor", false);
            }
        }

        double _currentDelta = 0.0;
        double _initDelta = 0.33;

        public override void _Process(double delta)
        {
            _currentDelta += delta;

            if (Singleton == null && _currentDelta >= _initDelta)
            {
                Initialize();
            }

            if (!_systems.Initialized)
            {
                return;
            }

            foreach (var system in _systems.Systems)
            {
                system.Process((float)delta);
            }
        }

        public void Export(bool debug)
        {
            PluginLogger.Type = PluginLogger.LoggerType.Toasts;
            Debug = debug;

            foreach (var system in _systems.Systems)
            {
                if (!system.PreExport())
                {
                    //return false;
                }
            }

            foreach (var system in _systems.Systems)
            {
                if (!system.Export())
                {
                    //return false;
                }
            }

            foreach (var system in _systems.Systems)
            {
                if (!system.PostExport())
                {
                    //return false;
                }
            }

            Debug = false;
            PluginLogger.Type = PluginLogger.LoggerType.Basic;

            //return true;
        }

        public override void _ExitTree()
        {
            _systems.Deinitialize();

            Singleton = null;

            PluginLogger.Deinitialize();

            Types.Deinitialize();
        }
    }
}

#endif
