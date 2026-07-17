// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;
using System.Collections.Generic;

namespace FuncGodot
{
    /// <summary>
    /// Per-machine settings, stored as JSON in <c>user://</c> rather than in the resource itself, so paths
    /// local to one developer's machine never end up in version control.
    /// <para>
    /// Do not create new instances: use <c>addons/func_godot_csharp/func_godot_local_config.tres</c>.
    /// </para>
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotLocalConfig : Resource
    {
        public enum Property
        {
            /// Where an exported FGD is written, unless a game config overrides it.
            FgdOutputFolder,

            /// Where the TrenchBroom game configuration is written.
            TrenchBroomGameConfigFolder,

            /// The mapping folder holding all map editor assets, usually the project folder or a subfolder.
            MapEditorGamePath,
        }

        public const string ConfigPath = "res://addons/func_godot_csharp/func_godot_local_config.tres";

        [Export(PropertyHint.GlobalDir)]
        public string FgdOutputFolder
        {
            get => Get(Property.FgdOutputFolder);
            set => Set(Property.FgdOutputFolder, value);
        }

        [Export(PropertyHint.GlobalDir)]
        public string TrenchBroomGameConfigFolder
        {
            get => Get(Property.TrenchBroomGameConfigFolder);
            set => Set(Property.TrenchBroomGameConfigFolder, value);
        }

        [Export(PropertyHint.GlobalDir)]
        public string MapEditorGamePath
        {
            get => Get(Property.MapEditorGamePath);
            set => Set(Property.MapEditorGamePath, value);
        }

        [ExportToolButton("Export func_godot settings", Icon = "Save")]
        public Callable ExportSettingsButton => Callable.From(ExportSettings);

        [ExportToolButton("Reload func_godot settings", Icon = "Reload")]
        public Callable ReloadSettingsButton => Callable.From(ReloadSettings);

        private readonly Dictionary<Property, string> _settings = [];
        private bool _loaded = false;

        /// Reads a setting from the local configuration file.
        public static string GetSetting(Property property)
        {
            if (ResourceLoader.Load(ConfigPath) is not FuncGodotLocalConfig config)
            {
                GD.PushError($"Failed to load local config at {ConfigPath}");
                return string.Empty;
            }

            config.ReloadSettings();
            return config.Get(property);
        }

        private string Get(Property property)
        {
            if (!_loaded)
            {
                ReloadSettings();
            }

            return _settings.GetValueOrDefault(property, string.Empty);
        }

        private void Set(Property property, string value)
        {
            _settings[property] = value;
        }

        /// Reloads the settings from this machine's configuration file.
        public void ReloadSettings()
        {
            _loaded = true;

            string path = GetConfigFilePath();

            if (!FileAccess.FileExists(path))
            {
                return;
            }

            string contents = FileAccess.GetFileAsString(path);

            if (string.IsNullOrEmpty(contents))
            {
                return;
            }

            Variant parsed = Json.ParseString(contents);

            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                GD.PushError($"Malformed local config at {path}");
                return;
            }

            _settings.Clear();

            Godot.Collections.Dictionary values = parsed.AsGodotDictionary();

            foreach (Property property in System.Enum.GetValues<Property>())
            {
                string key = property.ToString();

                if (values.TryGetValue(key, out Variant value))
                {
                    _settings[property] = value.AsString();
                }
            }

            NotifyPropertyListChanged();
        }

        /// Writes the current settings to this machine's configuration file.
        public void ExportSettings()
        {
            if (_settings.Count == 0)
            {
                return;
            }

            Godot.Collections.Dictionary values = [];

            foreach (KeyValuePair<Property, string> setting in _settings)
            {
                values[setting.Key.ToString()] = setting.Value;
            }

            string path = GetConfigFilePath();

            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

            if (file == null)
            {
                GD.PushError($"Failed to open local config for writing at {path}");
                return;
            }

            file.StoreLine(Json.Stringify(values));
            _loaded = false;

            GD.Print("Saved settings to ", file.GetPathAbsolute());
        }

        private static string GetConfigFilePath()
        {
            return "user://func_godot_config.json";
        }
    }
}
