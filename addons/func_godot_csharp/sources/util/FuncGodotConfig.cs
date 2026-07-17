// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Project-wide func_godot configuration, committed to version control. Holds the defaults that used to
    /// live under the <c>func_godot/</c> ProjectSettings prefix. Load the shared instance with <see cref="Load"/>
    /// rather than constructing new ones.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotConfig : Resource
    {
        public const string ConfigPath = "res://addons/func_godot_csharp/func_godot_config.tres";

        /// Map settings assigned to a newly created <see cref="FuncGodotMap"/> node.
        [Export]
        public FuncGodotMapSettings DefaultMapSettings;

        /// <summary>
        /// Inverse scale factor a <see cref="FuncGodotFGDModelPointClass"/> falls back to when its own scale
        /// expression is empty.
        /// </summary>
        [Export]
        public float DefaultInverseScaleFactor = 32.0f;

        /// Folder that <see cref="FuncGodotFGDModelPointClass"/> display models are exported into.
        [Export]
        public string ModelPointClassSavePath = string.Empty;

        /// Loads the shared config resource, or null when it is missing.
        public static FuncGodotConfig Load()
        {
            return ResourceLoader.Exists(ConfigPath) ? ResourceLoader.Load(ConfigPath) as FuncGodotConfig : null;
        }
    }
}
