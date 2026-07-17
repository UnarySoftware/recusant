// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

#if TOOLS

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// Editor plugin entry point. Registers the .map importer. Project-wide defaults live in the committed
    /// <see cref="FuncGodotConfig"/> resource rather than ProjectSettings.
    /// </summary>
    [Tool]
    public partial class FuncGodotPlugin : EditorPlugin
    {
        private QuakeMapImportPlugin _mapImportPlugin;

        public override string _GetPluginName()
        {
            return "FuncGodot";
        }

        public override bool _Handles(GodotObject @object)
        {
            return @object is FuncGodotMap;
        }

        public override void _EnterTree()
        {
            _mapImportPlugin = new QuakeMapImportPlugin();
            AddImportPlugin(_mapImportPlugin);
        }

        public override void _ExitTree()
        {
            if (_mapImportPlugin != null)
            {
                RemoveImportPlugin(_mapImportPlugin);
                _mapImportPlugin = null;
            }
        }
    }
}

#endif
